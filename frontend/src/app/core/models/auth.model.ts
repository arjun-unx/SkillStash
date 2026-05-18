export interface AuthResponse {
  userId: string;
  email: string;
  userName: string;
  displayName: string;
  accessToken: string;
  expiresAtUtc: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  userName: string;
  displayName: string;
  bio?: string | null;
  avatarUrl?: string | null;
  emailNotificationsEnabled: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  userName: string;
  displayName: string;
  password: string;
}
