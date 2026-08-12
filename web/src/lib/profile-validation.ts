import type { UpdateUserProfileInput } from "@/types/user";

export const BIO_MAX_LENGTH = 1000;
export const DISPLAY_NAME_MAX_LENGTH = 100;
export const USERNAME_MIN_LENGTH = 3;
export const USERNAME_MAX_LENGTH = 50;

// Mirrors UserService.ValidateUsername on the API, which stays the authority.
const USERNAME_PATTERN = /^[a-z0-9]([a-z0-9_-]*[a-z0-9])?$/;

export function validateProfile(input: UpdateUserProfileInput): string | null {
  const displayName = input.displayName.trim();
  const username = input.username.trim();

  if (!displayName) {
    return "Enter a display name.";
  }
  if (displayName.length > DISPLAY_NAME_MAX_LENGTH) {
    return `Display names must be ${DISPLAY_NAME_MAX_LENGTH} characters or fewer.`;
  }
  if (
    username.length < USERNAME_MIN_LENGTH ||
    username.length > USERNAME_MAX_LENGTH
  ) {
    return `Usernames must be between ${USERNAME_MIN_LENGTH} and ${USERNAME_MAX_LENGTH} characters.`;
  }
  if (!USERNAME_PATTERN.test(username)) {
    return "Usernames may only use lowercase letters, numbers, hyphens, and underscores, and must start and end with a letter or number.";
  }
  if ((input.bio ?? "").length > BIO_MAX_LENGTH) {
    return `Bios must be ${BIO_MAX_LENGTH} characters or fewer.`;
  }

  return null;
}
