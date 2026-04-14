# Zombie Survival - Apocalipse Online 🧟

Jogo de sobrevivência de apocalipse zumbi **3D FPS** que roda direto no navegador! Feito com **Three.js**, **JavaScript**, e **WebSocket** para multiplayer.

**Sem instalação necessária** — abra o `index.html` e jogue!

---

## Como Jogar

### Opção 1: Abrir direto no navegador
1. Abra o arquivo `index.html` no seu navegador (Chrome/Firefox/Edge)
2. Clique em **JOGAR**
3. Clique na tela para ativar o mouse lock
4. Sobreviva!

### Opção 2: Com servidor local (recomendado)
```bash
# Na pasta do projeto, rode um servidor HTTP simples:
npx serve .
# ou
python3 -m http.server 8080
```
Depois abra `http://localhost:3000` ou `http://localhost:8080` no navegador.

### Multiplayer
```bash
# Instale as dependências do servidor
cd server && npm install

# Rode o servidor WebSocket
npm start
```
No jogo, clique em **MULTIPLAYER**, insira o endereço `ws://localhost:3000`, e clique **HOSPEDAR** ou **CONECTAR**.

---

## Controles

| Tecla | Ação |
|-------|------|
| **WASD** | Mover |
| **Mouse** | Olhar/Mirar |
| **Clique Esquerdo** | Atirar |
| **Clique Direito** | Mira (ADS) |
| **Shift** | Correr |
| **C / Ctrl** | Agachar |
| **Espaço** | Pular |
| **R** | Recarregar |
| **1, 2, 3** | Trocar arma |
| **Scroll** | Trocar arma |
| **Esc** | Pausar |

---

## Funcionalidades

### Gameplay
- **Controle FPS completo** — mover, correr, agachar, pular, head bobbing
- **Sistema de vida** com regeneração automática
- **Sistema de stamina** para sprint
- **6 tipos de armas** — Pistola, Rifle, Shotgun, SMG, Sniper, Faca
- **ADS (mira)** com zoom e spread reduzido
- **Recoil realista** por tipo de arma
- **Pickups** — vida, munição, armas novas espalhadas pelo mapa

### Zombies (IA)
- **5 tipos de zombies:**
  - 🟢 Walker — lento, básico
  - 🔴 Runner — rápido, frágil
  - 🟤 Tank — lento, muita vida
  - 🟡 Spitter — ataque à distância
  - 🟣 Screamer — alerta outros zombies
- **Waves progressivas** com dificuldade crescente
- **IA com detecção** — perseguem, atacam, vagueiam
- **Animações** — andar, correr, atacar, morrer
- **Ragdoll** na morte

### Mundo
- **Cidade 3D completa** — prédios, carros, barricadas, postes, lixeiras
- **Ciclo dia/noite** com iluminação dinâmica
- **Neblina** atmosférica
- **Barris de fogo** com iluminação

### Ranking
- **Leaderboard persistente** (salvo no navegador)
- **Score** por kills, headshots, waves
- **Ranking** por score, kills, waves, tempo de sobrevivência
- **Top 3** com destaque (ouro, prata, bronze)

### Áudio
- **Sons procedurais** — tiros, zombies, passos, pickups, UI
- **Sons diferentes** por tipo de arma
- **Sons de zombie** — groaning, ataque, morte, detecção
- **Heartbeat** quando a vida está baixa
- **Controle de volume** — Master, Música, SFX separados

### UI
- **HUD completo** — vida, stamina, munição, wave, score, crosshair
- **Hit markers** normais e headshot
- **Overlay de dano** (tela vermelha)
- **Notificação de wave**
- **Menu principal** com settings
- **Menu de pausa**
- **Tela de morte** com respawn timer
- **Tela de game over** com estatísticas completas
- **Leaderboard** com tabs de ordenação

### Multiplayer
- **WebSocket** server para até 16 jogadores
- **Chat** in-game
- **Sincronização** de posição e estado

---

## Tecnologias

- **Three.js** — Rendering 3D, iluminação, sombras
- **Web Audio API** — Sons procedurais, efeitos sonoros
- **WebSocket (ws)** — Multiplayer em tempo real
- **localStorage** — Persistência do leaderboard
- **HTML5 Canvas** — Rendering
- **Pointer Lock API** — Controle FPS do mouse

---

## Estrutura do Projeto

```
ZombieSurvivalWeb/
├── index.html              # Página principal
├── css/style.css           # Estilos da UI
├── js/
│   ├── main.js             # Ponto de entrada
│   ├── game/
│   │   ├── Game.js         # Controlador principal
│   │   └── Leaderboard.js  # Ranking e persistência
│   ├── player/
│   │   ├── PlayerController.js  # Movimento FPS
│   │   ├── PlayerHealth.js      # Sistema de vida
│   │   └── PlayerStamina.js     # Sistema de stamina
│   ├── weapons/
│   │   └── WeaponSystem.js      # 6 armas com tiro, recarga, ADS
│   ├── zombie/
│   │   ├── ZombieAI.js          # IA com 5 tipos
│   │   └── ZombieSpawner.js     # Sistema de waves
│   ├── environment/
│   │   ├── Environment.js       # Cidade 3D (prédios, carros, etc.)
│   │   └── DayNightCycle.js     # Ciclo dia/noite
│   ├── audio/
│   │   └── AudioManager.js      # Sons procedurais
│   ├── ui/
│   │   ├── HUD.js              # HUD do jogo
│   │   └── MenuManager.js      # Menus e telas
│   ├── network/
│   │   └── MultiplayerClient.js # WebSocket client
│   └── utils/
│       └── MathUtils.js         # Utilitários
└── server/
    ├── server.js           # Servidor WebSocket
    └── package.json        # Dependências do servidor
```

---

*Feito com ❤ por Bruno*
