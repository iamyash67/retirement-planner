export interface User {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  age: number;
  gender: string | null;
}

/** Returned by register, login and refresh. The refresh token travels only in an httpOnly cookie. */
export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string; // yyyy-MM-dd
  gender?: string | null;
}
