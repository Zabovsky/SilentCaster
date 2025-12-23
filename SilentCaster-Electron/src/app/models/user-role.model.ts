export enum UserRole {
  VIEWER = 'viewer',
  SUBSCRIBER = 'subscriber',
  VIP = 'vip',
  MODERATOR = 'moderator',
  ADMIN = 'admin',
  BROADCASTER = 'broadcaster'
}

export interface UserPermissions {
  canModerate: boolean;
  canDeleteMessages: boolean;
  canTimeout: boolean;
  canBan: boolean;
  canClearChat: boolean;
  canWhisper: boolean;
  canManageSettings: boolean;
  canManageVoiceProfiles: boolean;
  canManageResponses: boolean;
}

export class PermissionManager {
  static getPermissions(role: UserRole): UserPermissions {
    switch (role) {
      case UserRole.BROADCASTER:
      case UserRole.ADMIN:
        return {
          canModerate: true,
          canDeleteMessages: true,
          canTimeout: true,
          canBan: true,
          canClearChat: true,
          canWhisper: true,
          canManageSettings: true,
          canManageVoiceProfiles: true,
          canManageResponses: true
        };
      
      case UserRole.MODERATOR:
        return {
          canModerate: true,
          canDeleteMessages: true,
          canTimeout: true,
          canBan: true,
          canClearChat: true,
          canWhisper: true,
          canManageSettings: false,
          canManageVoiceProfiles: false,
          canManageResponses: false
        };
      
      case UserRole.VIP:
      case UserRole.SUBSCRIBER:
        return {
          canModerate: false,
          canDeleteMessages: false,
          canTimeout: false,
          canBan: false,
          canClearChat: false,
          canWhisper: false,
          canManageSettings: false,
          canManageVoiceProfiles: false,
          canManageResponses: false
        };
      
      case UserRole.VIEWER:
      default:
        return {
          canModerate: false,
          canDeleteMessages: false,
          canTimeout: false,
          canBan: false,
          canClearChat: false,
          canWhisper: false,
          canManageSettings: false,
          canManageVoiceProfiles: false,
          canManageResponses: false
        };
    }
  }

  static hasPermission(role: UserRole, permission: keyof UserPermissions): boolean {
    const permissions = this.getPermissions(role);
    return permissions[permission];
  }
}

