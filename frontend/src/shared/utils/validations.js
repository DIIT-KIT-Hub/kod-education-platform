export const validateLatin = (text) => /^[A-Za-z\s]+$/.test(text);

export const validateEmail = (email) =>
  /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);

export const validatePassword = (password) =>
  /^(?=.*[A-Z])(?=.*\d)[A-Za-z\d]$/.test(password);
