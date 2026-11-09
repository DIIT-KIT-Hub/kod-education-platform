using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Identity;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Email;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Text;

namespace ODC.UnitTests.Web;

/// <summary>
/// Rules many features rely on: the file type allow-lists, e-mail address normalization, domain failures as results,
/// account e-mails (encoded names, no code in the subject, the recipient's language) and the interface language of a
/// request.
/// </summary>
public sealed class SharedRulesTests
{
    [Theory]
    [InlineData("order.pdf", "application/pdf")]
    [InlineData("ORDER.PDF", "application/pdf")]
    [InlineData("акт приймання.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("scan.jpeg", "image/jpeg")]
    [InlineData("archive.zip", "application/zip")]
    public void Document_files_of_the_allowed_types_get_their_content_type(string name, string type)
    {
        Assert.Equal(type, FileRules.ResolveDocumentType(name));
    }

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("page.html")]
    [InlineData("image.svg")]
    [InlineData("script.js")]
    [InlineData("order.pdf.exe")]
    [InlineData("pdf")]
    [InlineData("order.")]
    [InlineData("")]
    [InlineData("photo.webp")]
    public void Other_document_files_are_refused(string name)
    {
        Assert.Null(FileRules.ResolveDocumentType(name));
    }

    [Theory]
    [InlineData("voice.m4a", "audio/mp4")]
    [InlineData("signed.p7s", "application/pkcs7-signature")]
    [InlineData("container.ASICE", "application/vnd.etsi.asic-e+zip")]
    [InlineData("photo.heic", "image/heic")]
    public void Chat_files_of_the_allowed_types_get_their_content_type(string name, string type)
    {
        Assert.Equal(type, FileRules.ResolveChatFileType(name));
    }

    [Theory]
    [InlineData("run.bat")]
    [InlineData("tool.exe")]
    [InlineData("page.htm")]
    [InlineData("vector.svg")]
    [InlineData("macro.docm")]
    [InlineData("app.apk")]
    public void Executable_or_scriptable_chat_files_are_refused(string name)
    {
        Assert.Null(FileRules.ResolveChatFileType(name));
    }

    [Fact]
    public void Chat_photos_are_a_subset_of_the_chat_files_and_never_scriptable()
    {
        Assert.All(FileRules.ChatPhotoExtensions, static e => Assert.True(FileRules.ChatFileTypes.ContainsKey(e)));
        Assert.DoesNotContain(".svg", (IEnumerable<string>)FileRules.ChatPhotoExtensions);
        Assert.DoesNotContain(".gif", (IEnumerable<string>)FileRules.ChatPhotoExtensions);
    }

    [Fact]
    public void The_accept_list_of_documents_names_every_allowed_extension_in_order()
    {
        var accepted = FileRules.DocumentAccept.Split(',');

        Assert.Equal(FileRules.DocumentTypes.Keys.Order(StringComparer.Ordinal), accepted);
    }

    [Theory]
    [InlineData(" Ivan.Petrenko@Example.COM ", "ivan.petrenko@example.com")]
    [InlineData("o'brien+tag@sub.example.ua", "o'brien+tag@sub.example.ua")]
    public void An_e_mail_address_is_trimmed_and_lower_cased(string value, string expected)
    {
        Assert.Equal(expected, EmailAddress.Normalize(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not an address")]
    [InlineData("Ivan <ivan@example.com>")]
    [InlineData("ivan@example.com, olena@example.com")]
    [InlineData("ivan@")]
    [InlineData("@example.com")]
    public void Anything_but_a_single_plain_address_is_refused(string? value)
    {
        Assert.Null(EmailAddress.Normalize(value));
    }

    [Fact]
    public void An_address_longer_than_the_column_is_refused()
    {
        Assert.Null(EmailAddress.Normalize(new string('a', User.EmailMaxLength) + "@example.com"));
    }

    [Fact]
    public async Task A_broken_domain_rule_becomes_a_validation_error()
    {
        var result = await DomainErrors.GuardAsync(static () => throw new DomainException("Назва обов'язкова."));

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Equal("Назва обов'язкова.", result.Error?.Message);
    }

    [Fact]
    public async Task A_concurrent_change_becomes_a_conflict()
    {
        var result = await DomainErrors.GuardAsync<int>(static () => throw new DbUpdateConcurrencyException());

        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Equal("Дані щойно змінив інший користувач. Оновіть сторінку та спробуйте ще раз.", result.Error?.Message);
    }

    [Fact]
    public async Task Other_exceptions_are_not_hidden_and_success_passes_through()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(static () => DomainErrors.GuardAsync(static () => throw new InvalidOperationException()));

        Assert.Equal(5, (await DomainErrors.GuardAsync(static () => Task.FromResult(Result<int>.Success(5)))).Value);
    }

    [Fact]
    public void Names_in_account_e_mails_are_encoded_and_the_link_leads_to_the_activation_page()
    {
        var user = new User("ivan@example.com", "<b>Петренко</b>", "<script>alert(1)</script>", null);

        var mail = AccountEmails.Invitation(user, "https://cabinet.example.com/");

        Assert.DoesNotContain("<script>", mail.Html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", mail.Html, StringComparison.Ordinal);
        Assert.Contains("https://cabinet.example.com/account/activate", mail.Html, StringComparison.Ordinal);
        Assert.Contains("https://cabinet.example.com/account/activate", mail.Text, StringComparison.Ordinal);
        Assert.Equal("Ваш обліковий запис у КОД створено", mail.Subject);
        Assert.Equal("ivan@example.com", mail.To);
    }

    [Fact]
    public void The_activation_code_is_in_the_body_never_in_the_subject()
    {
        var user = new User("ivan@example.com", "Петренко", "Іван", null);

        var mail = AccountEmails.ActivationCode(user, "482913", TimeSpan.FromSeconds(570), "https://cabinet.example.com");

        Assert.DoesNotContain("482913", mail.Subject, StringComparison.Ordinal);
        Assert.Contains("482913", mail.Html, StringComparison.Ordinal);
        Assert.Contains("482913", mail.Text, StringComparison.Ordinal);
        Assert.Contains("10 хв", mail.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Account_e_mails_follow_the_recipients_language_and_restore_the_culture_of_the_request()
    {
        var user = new User("john@example.com", "Smith", "John", null);
        user.SetLanguage("en");
        var before = CultureInfo.CurrentUICulture;

        var invitation = AccountEmails.Invitation(user, "https://cabinet.example.com");
        var code = AccountEmails.ActivationCode(user, "111111", TimeSpan.FromMinutes(10), "https://cabinet.example.com");

        Assert.Equal("Your ODC account has been created", invitation.Subject);
        Assert.Equal("ODC account activation code", code.Subject);
        Assert.Equal(before, CultureInfo.CurrentUICulture);
        Assert.NotEqual(invitation.Html, AccountEmails.PasswordReset(user, "https://cabinet.example.com").Html);
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("EN-gb", "en")]
    [InlineData("uk_UA", "uk")]
    [InlineData(" uk ", "uk")]
    [InlineData("de", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Language_codes_are_normalized_to_the_supported_ones(string? value, string? expected)
    {
        Assert.Equal(expected, Languages.Normalize(value));
    }

    [Fact]
    public async Task The_profile_language_of_the_signed_in_user_wins_over_the_cookie()
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(AppClaimTypes.Language, "en")], "test")) };
        http.Request.Headers.Cookie = $"{LanguageCultureProvider.CookieName}=uk";

        var result = await new LanguageCultureProvider().DetermineProviderCultureResult(http);

        Assert.Equal("en-GB", Assert.Single(result!.UICultures).Value);
    }

    [Fact]
    public async Task Without_a_profile_language_the_cookie_decides_and_otherwise_the_default_applies()
    {
        var withCookie = new DefaultHttpContext();
        withCookie.Request.Headers.Cookie = $"{LanguageCultureProvider.CookieName}=en";
        var withJunk = new DefaultHttpContext();
        withJunk.Request.Headers.Cookie = $"{LanguageCultureProvider.CookieName}=<script>";

        Assert.Equal("en-GB", Assert.Single((await new LanguageCultureProvider().DetermineProviderCultureResult(withCookie))!.Cultures).Value);
        Assert.Null(await new LanguageCultureProvider().DetermineProviderCultureResult(withJunk));
        Assert.Null(await new LanguageCultureProvider().DetermineProviderCultureResult(new DefaultHttpContext()));
    }

    [Fact]
    public void The_language_cookie_is_a_secure_host_cookie_for_a_year()
    {
        var http = new DefaultHttpContext();

        LanguageCultureProvider.Remember(http.Response, "en");

        var cookie = http.Response.Headers.SetCookie.ToString();
        Assert.StartsWith("__Host-odc.lang=en", cookie, StringComparison.Ordinal);
        Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=31536000", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
