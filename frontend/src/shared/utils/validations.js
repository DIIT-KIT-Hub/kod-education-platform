export const validateLatin = (text) => /^[A-Za-z\s]+$/.test(text);

export const validateEmail = (email) =>
  /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);

export const validateUppercase = (text) => /[A-Z]/.test(text);

export const validateDigit = (text) => /[0-9]/.test(text);

export const validatePasswordAllowedChars = (password) =>
  /^[A-Za-z0-9]*$/.test(password);

export const validateLength = (text, minLength, maxLength) =>
  text.length >= minLength && text.length <= maxLength;
