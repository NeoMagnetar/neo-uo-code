# Phase64D1C Client Build Notes

## Source

- Source lane: pinned ClassicUO commit `a7cf920e42436ce62b9f26b846da8665c9fd3364`
- Changed source: `src/ClassicUO.Client/Game/UI/Gumps/PaperdollGump.cs`
- Publication overlay: `client/classicuo/overlays/src/ClassicUO.Client/Game/UI/Gumps/PaperdollGump.cs`

## Client Publish

```powershell
dotnet publish "src/ClassicUO.Client/ClassicUO.Client.csproj" -c Release -r "win-x64" -o "dist-phase64d1c-client" /p:IS_DEV_BUILD=true /p:AssemblyVersion=1.1.0.301 /p:FileVersion=1.1.0.301 /p:NativeLib=Shared /p:OutputType=Library
```

## Combined Publish

The combined output was built in the official order:

1. Publish bootstrap.
2. Publish client library into the same combined output.

## Results

- NativeAOT toolchain available.
- Client publish: 0 errors.
- Combined publish: 0 errors.
- No new warning codes or messages introduced by the D1C change.
- Output `cuo.dll`: `722D511EC94B6C6C10989454BEE3659E7E61075E27CB5C7B589E05100DED09FD`
- Output `cuo.pdb`: `9F759AAA665A5807ED9146D1A1FA55848C5AB6D594363B8A607CEBB8AD833BE1`
- SourceLink commit: `a7cf920e42436ce62b9f26b846da8665c9fd3364`

Built outputs are intentionally not committed.
