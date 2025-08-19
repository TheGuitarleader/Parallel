// Copyright 2025 Kyle Ebbinga

#pragma once
#include <shobjidl.h>

class BackupCommand : public IExplorerCommand
{
public:
    HRESULT Invoke(IShellItemArray *psiItemArray, IBindCtx *pbc) override;
    HRESULT GetTitle(IShellItemArray *, LPWSTR *ppszName) override;
    HRESULT GetIcon(IShellItemArray *, LPWSTR *ppszIcon) override;
};
