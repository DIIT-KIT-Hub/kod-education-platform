/**
 * Validates that the input contains only Latin letters and spaces.
 *
 * @param {string} text - The input string to validate.
 * @returns {boolean} True if the string contains only A–Z, a–z, and spaces.
 */
export const validateLatin = (text) => /^[A-Za-z\s]+$/.test(text);

/**
 * Validates an email address format.
 *
 * @param {string} email - The email string to validate.
 * @returns {boolean} True if the email format is valid.
 */
export const validateEmail = (email) =>
  /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);

/**
 * Checks if the string contains at least one uppercase letter.
 *
 * @param {string} text - The input string to validate.
 * @returns {boolean} True if at least one uppercase letter (A–Z) is present.
 */
export const validateUppercase = (text) => /[A-Z]/.test(text);

/**
 * Checks if the string contains at least one digit.
 *
 * @param {string} text - The input string to validate.
 * @returns {boolean} True if at least one numeric digit (0–9) is present.
 */
export const validateDigit = (text) => /[0-9]/.test(text);

/**
 * Validates that the password contains only allowed characters (Latin letters and digits).
 *
 * @param {string} password - The password string to validate.
 * @returns {boolean} True if the password contains only A–Z, a–z, and 0–9.
 */
export const validatePasswordAllowedChars = (password) =>
  /^[A-Za-z0-9]*$/.test(password);

/**
 * Validates that the string length is within the specified range.
 *
 * @param {string} text - The input string to validate.
 * @param {number} minLength - The minimum allowed length.
 * @param {number} maxLength - The maximum allowed length.
 * @returns {boolean} True if the string length is between minLength and maxLength (inclusive).
 */
export const validateLength = (text, minLength, maxLength) =>
  text.length >= minLength && text.length <= maxLength;
