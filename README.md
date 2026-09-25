# Kitsune Player

Leitor de música local para Windows, com letras sincronizadas e inspirado no Lark Player (Android). Desenvolvido em WPF/.NET 8, com temática de folclore japonês (Kitsune) — simplicidade e beleza visual em primeiro lugar.

Reescrito inteiramente do zero, com foco em compreender cada peça do código em vez de depender de frameworks prontos.

> **Status:** em desenvolvimento ativo — a primeira release pública será uma **beta**.

## Funcionalidades

- Biblioteca local com deteção automática de pastas (`FileSystemWatcher`) e reconexão automática de dispositivos USB
- Letras sincronizadas (`.lrc`) com auto-scroll, click-to-seek e edição direta; parser cascata: metadados embutidos → `.lrc` → `.kc.lrc` → API do LRCLIB
- Playlists (criar, editar, adicionar/remover músicas) e Favoritos como playlist de sistema
- Mini-player em janela separada, sempre visível (Topmost)
- Editar/eliminar músicas — metadados via tags do ficheiro, eliminação segura para a Reciclagem do Windows
- Equalizador com 20 presets de 10 bandas
- Pesquisa e ordenação por texto, com barra alfabética A-Z
- Vista em lista ou grelha, virtualizada para bibliotecas grandes
- 8 temas visuais: Zenko, Yako, Negitsune, Asa, Yoru, Tenko, Kuko, Reiko
- Multilíngue: Português, Inglês, Japonês, Espanhol
- Modo de reprodução de vídeo *(em desenvolvimento)*

## Arquitetura e tecnologias

| Área | Tecnologia |
|---|---|
| UI | WPF (.NET 8, `net8.0-windows10.0.22621.0`) |
| Padrão | MVVM (CommunityToolkit.Mvvm 8.4.2) + Injeção de Dependências (Microsoft.Extensions.DependencyInjection) |
| Áudio | ppy.ManagedBass — BASS + BASS_FX + BASSmix, com crossfade real |
| Vídeo | LibVLCSharp 3.10.x (motor separado, isolado por responsabilidade) |
| Metadados | TagLibSharp 2.3.0 |
| Base de dados | SQLite (Microsoft.Data.Sqlite) — o sistema de ficheiros é a fonte de verdade |
| Ícones | MahApps.Metro.IconPacks (BoxIcons), HandyControl seletivo |

Serviços e ViewModels partilhados (`PlayerViewModel`, `ShellViewModel`) são registados como *singletons* no contentor de DI.

## Requisitos

- Windows 10/11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## Compilar a partir do código

```bash
git clone https://github.com/AiroScript95/Kitsune-Player.git
```

Abrir a solução no Visual Studio 2022 (ou superior), restaurar os pacotes NuGet e compilar.

## Licença

Este projeto está licenciado sob a [MIT License](LICENSE.txt).
