## 🔮 Multi-Source Roadmap

NekoDex interfaces with each platform through its own dedicated provider. The catalog below reflects the current state: connected, paused, or open.

### ⚡ REST / JSON APIs (nhentai-Style)
- `nhentai` *(Active)*
- `HentaiFox` | `HentaiHand` | `HentaiEra` | `HentaiNexus` *(Active)*
- `3Hentai` | `Asmhentai` *(Active)*

### 🏷️ GData & Namespace Parsing (E-Hentai-Style)
- `E-Hentai` *(Active, Direct Search, `next=` offset)*
- `ExHentai` *(Paused, cookie session present, disabled in source selector)*
- `LoFi E-Hentai` | `HentaiDB` | `Doujins.com` *(Active)*

### 📦 Static CDN & Binary Index (Hitomi-Style)
- `Hitomi.la` *(Active, 30 IDs per page)*
- `SimplyHentai` *(Active)*
- `Kemono` *(Active, `kemono.cr`)*
- `MangaDex` *(Prepared)*

### 🔧 Custom / Direct Scraper
- `HentaiEnvy` | `HentaiClap` | `HentaiRun` *(Active, custom parsers)*

### 🗑️ Removed
- `HBrowse` | `Hentai2Read` | `DoujinMoe` | `HentaiRead`

## 🧹 Cleanup & Source Architecture

Dead and unstable sources have been removed from the enum, catalog, and web search. Associated rows in `online_site_tags` are purged upon opening the database (`hentai2read`, `doujinmoe`, `hbrowse`, `hentairead`).

- `HentaiEnvoy` is now consistently named `HentaiEnvy`. Legacy names (`hentaienvoy`) resolve to `hentaienvy.com` via `SourceCatalog.FromName`.
- `HentaiRun` parses the Next.js catalog on `https://hentairun.com/`. Single tags route to `/tag/{slug}/` and `/tag/{slug}/page/2/`, while multiple tags route to `/search/?q=`. Cards point to `/gallery/{id}/`, and covers reside on `m*.hentairun.com`.
- `HentaiClap` parses the PHP catalog on `https://hentaiclap.com/`. Searches run via `/search/?key=` and `&page=`. Individual tag paths like `/tag/anal/` return a 404; the index is
