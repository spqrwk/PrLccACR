# lib/ — CI 编译引用 DLL

本目录存放 GitHub Actions Runner 编译所需的引用 DLL。
Runner 是干净的 Windows 环境，没有 Dalamud 或 PromeRotation 安装。

## 来源

| DLL | 来源 |
|---|---|
| `Dalamud.dll` | XIVLauncher 开发目录 `addon/Hooks/dev/` |
| `Dalamud.Bindings.ImGui.dll` | 同上 |
| `Lumina.dll` | 同上 |
| `Lumina.Excel.dll` | 同上 |
| `ECommons.dll` | PromeRotation 安装目录 |
| `PromeRotation.dll` | PromeRotation 安装目录 |

## 更新方式

本地编译时，MSBuild Target `SyncReferenceDllsToLib` 会自动将本机最新 DLL 复制到本目录。
只需正常 `dotnet build`，然后提交变更：

```powershell
dotnet build MCH/MCH.csproj
git add lib/
git commit -m "Update reference DLLs"
git push
```

## 注意

- 这些 DLL **不会**被打包进 `latest.zip`
- 它们仅在编译期使用（`<Private>false</Private>`）
- CI Runner 通过 `-p:DalamudReferenceRoot=lib -p:PromeRotationReferenceRoot=lib` 引用本目录
