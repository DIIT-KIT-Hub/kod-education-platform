using ODC.Domain.Common;
using ODC.Domain.Localization;

namespace ODC.Domain.Chats;

/// <summary>
/// A conversation: one-to-one (<see cref="ChatKind.Direct"/>, at most one per pair of people) or a group. Every message
/// gets the next number of the chat (<see cref="LastSequence"/>), so members read in one order, find what they missed
/// after a reconnect and count unread messages from their last read number.
/// </summary>
public sealed class Chat : AuditableEntity
{
    /// <summary>
    /// Maximum length of <see cref="Title"/>.
    /// </summary>
    public const int TitleMaxLength = 120;

    /// <summary>
    /// The largest number of members of a group chat.
    /// </summary>
    public const int MaxMembers = 250;

    /// <summary>
    /// The members.
    /// </summary>
    private readonly List<ChatMember> _members = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Chat"/> class.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <param name="title">The title of a group chat.</param>
    /// <param name="directKey">The pair key of a one-to-one chat.</param>
    /// <param name="createdById">The creator.</param>
    /// <param name="moment">The current moment.</param>
    private Chat(ChatKind kind, string? title, string? directKey, Guid createdById, DateTimeOffset moment)
    {
        Kind = kind;
        Title = title;
        DirectKey = directKey;
        CreatedById = createdById;
        LastActivityAt = moment;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Chat"/> class for EF Core materialization.
    /// </summary>
    private Chat()
    {
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    public ChatKind Kind { get; private set; }

    /// <summary>
    /// Gets the title of a group chat; <see langword="null"/> shows the members' names instead.
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// Gets the key of the pair of a one-to-one chat (<see cref="DirectKeyOf"/>), unique in the database.
    /// </summary>
    public string? DirectKey { get; private set; }

    /// <summary>
    /// Gets the creator.
    /// </summary>
    public Guid CreatedById { get; private set; }

    /// <summary>
    /// Gets the number of the newest message (0 while there are none).
    /// </summary>
    public long LastSequence { get; private set; }

    /// <summary>
    /// Gets the moment of the newest message, or of the creation (orders the chat list).
    /// </summary>
    public DateTimeOffset LastActivityAt { get; private set; }

    /// <summary>
    /// Gets the members.
    /// </summary>
    public IReadOnlyCollection<ChatMember> Members => _members;

    /// <summary>
    /// Builds the key of the one-to-one chat of two people (the same for both orders).
    /// </summary>
    /// <param name="first">One person.</param>
    /// <param name="second">The other person.</param>
    /// <returns>The key.</returns>
    public static string DirectKeyOf(Guid first, Guid second)
        => first.CompareTo(second) < 0 ? $"{first:N}:{second:N}" : $"{second:N}:{first:N}";

    /// <summary>
    /// Starts a one-to-one chat.
    /// </summary>
    /// <param name="starterId">The person who starts it.</param>
    /// <param name="peerId">The other person.</param>
    /// <param name="moment">The current moment.</param>
    /// <returns>The chat.</returns>
    public static Chat Direct(Guid starterId, Guid peerId, DateTimeOffset moment)
    {
        if (starterId == peerId)
        {
            throw new DomainException(Texts.T("Не можна почати чат із самим собою."));
        }

        var chat = new Chat(ChatKind.Direct, null, DirectKeyOf(starterId, peerId), starterId, moment);
        chat._members.Add(new ChatMember(chat.Id, starterId, ChatMemberRole.Member, moment));
        chat._members.Add(new ChatMember(chat.Id, peerId, ChatMemberRole.Member, moment));
        return chat;
    }

    /// <summary>
    /// Creates a group chat; the creator becomes its owner.
    /// </summary>
    /// <param name="creatorId">The creator.</param>
    /// <param name="title">The title, or <see langword="null"/> to show the members' names.</param>
    /// <param name="memberIds">The other members.</param>
    /// <param name="moment">The current moment.</param>
    /// <returns>The chat.</returns>
    public static Chat Group(Guid creatorId, string? title, IEnumerable<Guid> memberIds, DateTimeOffset moment)
    {
        ArgumentNullException.ThrowIfNull(memberIds);

        var others = memberIds.Where(id => id != creatorId).Distinct().ToList();
        if (others.Count == 0)
        {
            throw new DomainException(Texts.T("Додайте до чату хоча б одну людину."));
        }

        if (others.Count + 1 > MaxMembers)
        {
            throw new DomainException(Texts.T("У груповому чаті може бути не більше {0} учасників.", MaxMembers));
        }

        var chat = new Chat(ChatKind.Group, Guard.OptionalText(title, TitleMaxLength, "Назва чату"), null, creatorId, moment);
        chat._members.Add(new ChatMember(chat.Id, creatorId, ChatMemberRole.Owner, moment));
        chat._members.AddRange(others.Select(id => new ChatMember(chat.Id, id, ChatMemberRole.Member, moment)));
        return chat;
    }

    /// <summary>
    /// Finds the membership of a person.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <returns>The membership, or <see langword="null"/> when the person is not a member.</returns>
    public ChatMember? MemberOf(Guid userId) => _members.Find(m => m.UserId == userId);

    /// <summary>
    /// Renames a group chat.
    /// </summary>
    /// <param name="title">The new title, or <see langword="null"/> to show the members' names.</param>
    public void Rename(string? title)
    {
        EnsureGroup();
        Title = Guard.OptionalText(title, TitleMaxLength, "Назва чату");
    }

    /// <summary>
    /// Adds people to a group chat; people who are already members are skipped.
    /// </summary>
    /// <param name="userIds">The people.</param>
    /// <param name="moment">The current moment.</param>
    /// <returns>The people actually added.</returns>
    public IReadOnlyList<Guid> AddMembers(IEnumerable<Guid> userIds, DateTimeOffset moment)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        EnsureGroup();

        var added = userIds.Distinct().Where(id => MemberOf(id) is null).ToList();
        if (_members.Count + added.Count > MaxMembers)
        {
            throw new DomainException(Texts.T("У груповому чаті може бути не більше {0} учасників.", MaxMembers));
        }

        // New members start as having read everything before them.
        _members.AddRange(added.Select(id => new ChatMember(Id, id, ChatMemberRole.Member, moment, LastSequence)));
        return added;
    }

    /// <summary>
    /// Removes a person from a group chat: anyone may leave, only the owner removes others. When the owner leaves, the
    /// longest member becomes the owner.
    /// </summary>
    /// <param name="actorId">The person who acts.</param>
    /// <param name="userId">The person who leaves or is removed.</param>
    public void RemoveMember(Guid actorId, Guid userId)
    {
        EnsureGroup();

        var member = MemberOf(userId) ?? throw new DomainException(Texts.T("Цієї людини немає в чаті."));
        if (actorId != userId && MemberOf(actorId)?.Role != ChatMemberRole.Owner)
        {
            throw new DomainException(Texts.T("Вилучати людей із чату може лише його власник."));
        }

        _members.Remove(member);
        if (member.Role == ChatMemberRole.Owner && _members.Count > 0 && !_members.Exists(static m => m.Role == ChatMemberRole.Owner))
        {
            _members.OrderBy(static m => m.JoinedAt).First().MakeOwner();
        }
    }

    /// <summary>
    /// Takes the number of a new message.
    /// </summary>
    /// <param name="moment">The moment of the message.</param>
    /// <returns>The number.</returns>
    public long NextSequence(DateTimeOffset moment)
    {
        LastSequence++;
        LastActivityAt = moment;
        return LastSequence;
    }

    /// <summary>
    /// Refuses changes that only a group chat allows.
    /// </summary>
    private void EnsureGroup()
    {
        if (Kind != ChatKind.Group)
        {
            throw new DomainException(Texts.T("Це можна зробити лише в груповому чаті."));
        }
    }
}
