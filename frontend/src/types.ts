export type TaskStatus = 'Pending' | 'InProgress' | 'Done';
export type TaskPriority = 'Low' | 'Medium' | 'High';
export type UserRole = 'User' | 'Admin';

export interface TaskItem { id: string; title: string; description: string; status: TaskStatus; priority: TaskPriority; createdAt: string; userId: string }
export interface User { id: string; name: string; email: string; role: UserRole; createdAt: string }
export interface AuthResponse { accessToken: string; refreshToken: string; accessTokenExpiresAt: string }
export interface RegisterRequest { name: string; email: string; password: string }
export interface LoginRequest { email: string; password: string }
export interface CreateTaskRequest { title: string; description: string; priority: TaskPriority }
export interface UpdateTaskStatusRequest { status: TaskStatus }

export const priorities: TaskPriority[] = ['Low', 'Medium', 'High'];
export const statuses: TaskStatus[] = ['Pending', 'InProgress', 'Done'];
