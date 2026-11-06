# RTSP Player

Aplicativo desktop nativo para Windows que reproduz uma câmera por meio de um endereço RTSP. O projeto usa WPF e LibVLCSharp, sem navegador ou WebView.

## Recursos

- Reprodução de streams RTSP no próprio aplicativo.
- Campo único para o endereço completo, incluindo usuário e senha quando necessários.
- Opção para salvar a conexão localmente, protegida pelo Windows DPAPI para o usuário atual.
- Controles para reconectar ou configurar outra câmera durante a reprodução.
- Janela redimensionável, com o viewer acompanhando a área disponível.

## Usar o aplicativo

1. Baixe a versão para Windows em [Releases](https://github.com/Brunomas1/RTSP-Player-Desktop/releases), se houver uma publicação disponível.
2. Extraia a pasta distribuída e abra `RTSP Player.exe`.
3. Cole o endereço RTSP completo. Exemplo com valores fictícios:

   ```text
   rtsp://usuario:senha@192.0.2.10:554/caminho/do/stream
   ```

4. Deixe **Salvar conexão** marcado para reutilizar o endereço na próxima abertura. Desmarque para não manter a conexão salva.

> `192.0.2.10`, `usuario`, `senha` e o caminho acima são exemplos reservados, não dados de uma câmera real. Se a senha contiver caracteres especiais, codifique-os conforme a sintaxe de URL.

O stream precisa estar acessível a partir do computador. O endereço, o caminho e as credenciais variam conforme a câmera ou o gravador.

## Credenciais e dados locais

Quando a opção de salvar está marcada, o aplicativo grava o endereço RTSP em `%LOCALAPPDATA%\CameraViewer\connection.json`. O valor é protegido com DPAPI no escopo do usuário atual do Windows. A proteção não sincroniza credenciais entre usuários ou computadores. Ao desmarcar a opção, o arquivo salvo é removido.

Não inclua links RTSP reais em issues, pull requests, capturas de tela, logs ou commits. Use endereços e credenciais fictícios ao compartilhar exemplos.

## Compilar

Requisitos para desenvolvimento: Windows, .NET 10 SDK e acesso ao NuGet para restaurar os pacotes.

```powershell
dotnet restore CameraViewer.csproj
dotnet build CameraViewer.csproj -c Release
```

Para publicar uma versão Windows x64 independente do runtime .NET:

```powershell
dotnet publish CameraViewer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish\win-x64
```

Distribua a pasta `publish\win-x64` completa. Além do executável, ela contém os plugins e arquivos nativos do VLC necessários para reproduzir vídeo.

## Dependências

- [LibVLCSharp](https://github.com/videolan/libvlcsharp)
- [VLC para Windows](https://www.videolan.org/vlc/)

Os pacotes usados pelo projeto são distribuídos sob LGPL-2.1-or-later. Consulte os avisos e termos de cada dependência antes de redistribuir o aplicativo ou seus componentes.

## Licença

O código deste projeto é disponibilizado sob a licença MIT. As dependências de terceiros mantêm suas próprias licenças. Consulte [`LICENSE`](LICENSE) e [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).
