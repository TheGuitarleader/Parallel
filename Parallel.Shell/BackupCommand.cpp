#include "pch.h"
#include "BackupCommand.h"
#include <strsafe.h>

#include "resource.h"

HRESULT BackupCommand::Invoke(IShellItemArray* psiItemArray, IBindCtx* pbc)
{
    return S_OK;
}

HRESULT BackupCommand::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
    *ppszName = ::SysAllocString(L"Send to Service");
    return S_OK;
}

HRESULT BackupCommand::GetIcon(IShellItemArray* /*psiItemArray*/, PWSTR* ppszIcon)
{
    return S_OK;
}



