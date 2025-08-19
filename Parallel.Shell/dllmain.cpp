// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <windows.h>
#include <shlwapi.h>  // For RegDeleteTreeW
#include <strsafe.h>

HMODULE g_hModule = NULL;

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    if (ul_reason_for_call == DLL_PROCESS_ATTACH) {
        g_hModule = hModule;
    }
    return TRUE;
}

extern "C" __declspec(dllexport) HRESULT __stdcall DllRegisterServer() {
    WCHAR szModulePath[MAX_PATH];
    GetModuleFileNameW(g_hModule, szModulePath, MAX_PATH);

    // Build icon path: "<dll path>,0"
    WCHAR szIconPath[MAX_PATH + 10];
    StringCchPrintfW(szIconPath, ARRAYSIZE(szIconPath), L"%s,%d", szModulePath, 0); // icon index 0

    // Register CLSID
    HKEY hKey;
    LONG lResult = RegCreateKeyExW(HKEY_CLASSES_ROOT, L"CLSID\\{d194f491-9a76-44bd-84e1-62cdccd49bea}\\InprocServer32", 0, NULL, 0, KEY_WRITE, NULL, &hKey, NULL);
    if (lResult == ERROR_SUCCESS) {
        RegSetValueExW(hKey, NULL, 0, REG_SZ, (BYTE*)szModulePath, (DWORD)((wcslen(szModulePath) + 1) * sizeof(WCHAR)));
        RegSetValueExW(hKey, L"ThreadingModel", 0, REG_SZ, (BYTE*)L"Apartment", sizeof(L"Apartment"));
        RegCloseKey(hKey);
    }

    // CommandStore registration
    lResult = RegCreateKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\CommandStore\\shell\\SendToService", 0, NULL, 0, KEY_WRITE, NULL, &hKey, NULL);
    if (lResult == ERROR_SUCCESS) {
        RegSetValueExW(hKey, L"ExplorerCommandHandler", 0, REG_SZ, (BYTE*)L"{d194f491-9a76-44bd-84e1-62cdccd49bea}", sizeof(L"{d194f491-9a76-44bd-84e1-62cdccd49bea}"));
        RegSetValueExW(hKey, L"MUIVerb", 0, REG_SZ, (BYTE*)L"Parallel", sizeof(L"Parallel"));
        RegSetValueExW(hKey, L"Icon", 0, REG_SZ, (BYTE*)szIconPath, (DWORD)((wcslen(szIconPath) + 1) * sizeof(WCHAR)));
        RegCloseKey(hKey);
    }

    // 🔹 shellex registration for all files
    lResult = RegCreateKeyExW(HKEY_CLASSES_ROOT, L"*\\shellex\\ContextMenuHandlers\\Parallel", 0, NULL, 0, KEY_WRITE, NULL, &hKey, NULL);
    if (lResult == ERROR_SUCCESS) {
        RegSetValueExW(hKey, NULL, 0, REG_SZ, (BYTE*)L"{d194f491-9a76-44bd-84e1-62cdccd49bea}", sizeof(L"{d194f491-9a76-44bd-84e1-62cdccd49bea}"));
        RegCloseKey(hKey);
    }

    // 🔹 shellex registration for all folders
    lResult = RegCreateKeyExW(HKEY_CLASSES_ROOT, L"Directory\\shellex\\ContextMenuHandlers\\Parallel", 0, NULL, 0, KEY_WRITE, NULL, &hKey, NULL);
    if (lResult == ERROR_SUCCESS) {
        RegSetValueExW(hKey, NULL, 0, REG_SZ, (BYTE*)L"{d194f491-9a76-44bd-84e1-62cdccd49bea}", sizeof(L"{d194f491-9a76-44bd-84e1-62cdccd49bea}"));
        RegCloseKey(hKey);
    }

    return S_OK;
}

extern "C" __declspec(dllexport) HRESULT __stdcall DllUnregisterServer() {
    RegDeleteTreeW(HKEY_CLASSES_ROOT, L"CLSID\\{d194f491-9a76-44bd-84e1-62cdccd49bea}");
    RegDeleteTreeW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\CommandStore\\shell\\SendToService");
    RegDeleteTreeW(HKEY_CLASSES_ROOT, L"*\\shellex\\ContextMenuHandlers\\Parallel");
    RegDeleteTreeW(HKEY_CLASSES_ROOT, L"Directory\\shellex\\ContextMenuHandlers\\Parallel");
    return S_OK;
}
