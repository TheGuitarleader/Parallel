import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function generateHash(size: number): string {
  const chars = "abcdefghijklmnopqrstuvwxyz0123456789"
  let result = ""
  for (let i = 0; i < size; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length))
  }
  return result
}

export async function getMachineName(): Promise<string> {
  if (!window.electronAPI?.getMachineName) return "Default";

  try {
    const name = await window.electronAPI.getMachineName();
    return name ?? "Default"; // never null
  } catch (err) {
    console.error(err);
    return "Default";
  }
}