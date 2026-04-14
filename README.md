# 🧟 Zombie Survival - Apocalypse Online

Um jogo de sobrevivência de apocalipse zumbi multiplayer online feito com **Unity + C#**, utilizando **Mirror Networking** para multiplayer e **Universal Render Pipeline (URP)** para gráficos de alta qualidade.

![Zombie Survival](https://img.shields.io/badge/Unity-2022.3%20LTS-blue) ![C#](https://img.shields.io/badge/C%23-10.0-green) ![Mirror](https://img.shields.io/badge/Mirror-Networking-orange) ![URP](https://img.shields.io/badge/URP-14.0-purple)

---

## 🎮 Funcionalidades

### Core Gameplay
- **FPS Controller Completo**: Movimento, sprint, agachar, pular, head bobbing
- **Sistema de Vida e Stamina**: Regeneração, efeitos visuais de dano, estado crítico
- **Sistema de Armas**: Pistola, Rifle, Shotgun, SMG, Sniper, Melee (faca)
- **ADS (Aim Down Sights)**: Mira com zoom, spread reduzido
- **Recuo Realista**: Recoil com recuperação, spread baseado em movimento
- **Inventário de Armas**: 3 slots, troca por tecla numérica ou scroll

### Zombies (IA)
- **5 Tipos de Zombies**:
  - 🟢 **Walker** - Lento, básico
  - 🔴 **Runner** - Rápido, frágil
  - 🟤 **Tank** - Lento, muita vida
  - 🟡 **Spitter** - Ataque à distância
  - 🟣 **Screamer** - Alerta outros zombies
- **NavMesh AI**: Pathfinding inteligente com desvio de obstáculos
- **Sistema de Detecção**: Campo de visão + detecção por audição
- **Ragdoll Physics**: Física de ragdoll na morte dos zombies
- **Loot Drops**: Itens aleatórios ao matar zombies

### Sistema de Waves
- **Waves Progressivas**: Dificuldade crescente a cada wave
- **Spawn Inteligente**: Zombies spawnando fora da visão dos jogadores
- **Tipos Variados**: Mais tipos especiais em waves avançadas
- **Scaling**: Vida, dano e velocidade aumentam com as waves

### Multiplayer Online (Mirror)
- **Host/Join**: Um jogador hospeda, outros se conectam
- **Lobby System**: Sala de espera com sistema de ready
- **Chat In-Game**: Comunicação entre jogadores
- **Sincronização**: Posição, vida, armas, zombies sincronizados
- **Até 16 jogadores** simultâneos

### Ranking / Leaderboard
- **Score System**: Pontos por kills, waves sobrevividas
- **Leaderboard Persistente**: Ranking salvo entre sessões
- **Estatísticas Completas**: Kills, Deaths, K/D Ratio, Tempo de Sobrevivência
- **Ordenação**: Por score, kills ou tempo de sobrevivência
- **Top Ranks**: Destaque visual para top 3 (ouro, prata, bronze)

### Áudio
- **Audio Manager Completo**: Pool de SFX, música, ambiente
- **Sons Dinâmicos**: Música de combate vs calma
- **Efeitos Posicionais**: Sons 3D espacializados
- **Controle de Volume**: Master, Música, SFX, Ambiente separados
- **Sons de Ambiente**: Dia/noite diferentes

### Efeitos Visuais
- **Post-Processing (URP)**: Vignette, Chromatic Aberration, Bloom, Film Grain
- **Ciclo Dia/Noite**: Iluminação dinâmica, sol realista
- **Sistema de Clima**: Chuva, neve, neblina, tempestade com raios
- **Efeitos de Dano**: Overlay vermelho, chromatic aberration
- **Visão Noturna**: Toggle para exploração no escuro
- **Muzzle Flash**: Efeitos de tiro com ejeção de cartuchos

### Interface (UI)
- **HUD Completo**: Vida, stamina, munição, wave, score, minimap
- **Menu Principal**: Play, Settings, Credits
- **Lobby**: Lista de jogadores, ready system, customização
- **Tela de Morte**: Stats, respawn timer, espectador
- **Tela de Game Over**: Score final, ranking, estatísticas
- **Pausa**: Resume, settings, quit
- **Crosshair Dinâmico**: Hit marker normal e headshot
- **Scope Overlay**: Para sniper rifle

---

## 📁 Estrutura do Projeto

```
ZombieSurvival/
├── Assets/
│   ├── Scripts/
│   │   ├── Player/
│   │   │   ├── PlayerController.cs        # Movimento FPS, câmera
│   │   │   ├── PlayerHealth.cs            # Sistema de vida
│   │   │   ├── PlayerStamina.cs           # Sistema de stamina
│   │   │   ├── PlayerInteraction.cs       # Interação com objetos
│   │   │   ├── PlayerInventory.cs         # Inventário de armas
│   │   │   └── PlayerAnimationController.cs # Animações do jogador
│   │   ├── Zombie/
│   │   │   ├── ZombieAI.cs               # IA dos zombies (NavMesh)
│   │   │   ├── ZombieSpawner.cs          # Sistema de spawn/waves
│   │   │   └── ZombieAnimationController.cs # Animações/ragdoll
│   │   ├── Weapons/
│   │   │   ├── WeaponBase.cs             # Classe base de armas
│   │   │   └── WeaponTypes.cs            # Pistol, Rifle, Shotgun, SMG, Sniper, Melee
│   │   ├── Network/
│   │   │   ├── GameNetworkManager.cs     # Gerenciador de rede
│   │   │   ├── LobbyManager.cs           # Sistema de lobby
│   │   │   └── NetworkChat.cs            # Chat in-game
│   │   ├── Ranking/
│   │   │   └── LeaderboardSystem.cs      # Score + Leaderboard
│   │   ├── Audio/
│   │   │   ├── AudioManager.cs           # Gerenciador de áudio
│   │   │   └── AmbientSoundController.cs # Sons ambiente dinâmicos
│   │   ├── UI/
│   │   │   ├── GameHUD.cs               # HUD principal
│   │   │   ├── MainMenuUI.cs            # Menu principal
│   │   │   ├── LobbyUI.cs              # Interface do lobby
│   │   │   ├── LeaderboardUI.cs         # Ranking/leaderboard
│   │   │   ├── GameOverScreen.cs        # Tela de game over
│   │   │   └── DeathScreenAndPauseMenu.cs # Morte e pausa
│   │   ├── GameManager/
│   │   │   ├── GameManager.cs           # Controle geral do jogo
│   │   │   ├── DayNightCycle.cs         # Ciclo dia/noite
│   │   │   ├── WeatherSystem.cs         # Sistema de clima
│   │   │   ├── PickupItem.cs            # Itens coletáveis
│   │   │   └── PostProcessingController.cs # Efeitos visuais
│   │   └── Utils/
│   │       └── Utilities.cs             # Object pooling, extensões
│   ├── Scenes/
│   ├── Prefabs/
│   ├── Materials/
│   ├── Animations/
│   ├── Audio/
│   └── UI/
├── Packages/
│   └── manifest.json                    # Dependências Unity
├── ProjectSettings/
└── README.md
```

---

## 🚀 Como Configurar

### Pré-requisitos
- **Unity 2022.3 LTS** ou superior
- **Unity Hub** instalado
- **Mirror Networking** (via Asset Store ou Package Manager)

### Passo 1: Clonar o Repositório
```bash
git clone https://github.com/Bruningbr/ZombieSurvival.git
```

### Passo 2: Abrir no Unity
1. Abra o **Unity Hub**
2. Clique em **"Add"** → selecione a pasta do projeto
3. Abra o projeto (Unity vai importar automaticamente)

### Passo 3: Instalar Mirror Networking
1. No Unity, vá em **Window → Asset Store**
2. Busque **"Mirror"** (Mirror Networking - vis2k)
3. Clique em **Download** e **Import**
4. Ou via Package Manager: adicione `com.unity.mirror` ao manifest.json

### Passo 4: Configurar URP
1. Vá em **Edit → Project Settings → Graphics**
2. Defina o **URP Pipeline Asset** (criar um se necessário)
3. Ajuste as configurações de qualidade em **Edit → Project Settings → Quality**

### Passo 5: Importar Assets 3D
O jogo precisa de assets 3D para funcionar. Veja a seção de **Assets Recomendados** abaixo.

### Passo 6: Configurar Cenas
1. Crie as cenas: **MainMenu**, **Lobby**, **GameScene**
2. Adicione os prefabs e configure os componentes
3. Configure o **NavMesh** na cena de jogo (Window → AI → Navigation)

### Passo 7: Build
1. **File → Build Settings**
2. Adicione as cenas na ordem: MainMenu, Lobby, GameScene
3. Selecione a plataforma (PC, Mac, Linux)
4. Clique em **Build**

---

## 🎨 Assets Recomendados (Unity Asset Store)

### Gratuitos
| Asset | Descrição |
|-------|-----------|
| [Starter Assets - FPS](https://assetstore.unity.com/packages/essentials/starter-assets-first-person-character-controller-urp-196525) | Controller FPS oficial Unity |
| [Low Poly Zombie](https://assetstore.unity.com/packages/3d/characters/humanoids/zombie-30232) | Modelos de zombie gratuitos |
| [Free Urban Night Sky](https://assetstore.unity.com/packages/2d/textures-materials/sky/free-urban-night-sky-115283) | Skybox noturno |
| [FREE Stylized PBR Textures](https://assetstore.unity.com/packages/2d/textures-materials/free-stylized-pbr-textures-pack-111778) | Texturas PBR gratuitas |

### Pagos (Para Hiper-Realismo)
| Asset | Descrição | Preço ~USD |
|-------|-----------|------------|
| [Zombie Character Pack](https://assetstore.unity.com/packages/3d/characters/humanoids/zombie-character-pack-72857) | Pack de zombies realistas | $30-50 |
| [Modern Weapons Pack](https://assetstore.unity.com/packages/3d/props/guns/modern-weapons-pack-vol-1-140509) | Armas modernas com animações | $25-40 |
| [Urban Environment Pack](https://assetstore.unity.com/packages/3d/environments/urban/urban-environment-pack-216777) | Cenário urbano realista | $40-60 |
| [Mega Particle Effects](https://assetstore.unity.com/packages/vfx/particles/mega-particles-pack-55499) | Efeitos de partículas | $20-30 |
| [Universal Sound FX](https://assetstore.unity.com/packages/audio/sound-fx/universal-sound-fx-17256) | Pack de sons completo | $40 |

---

## 🎮 Controles

| Tecla | Ação |
|-------|------|
| **WASD** | Movimento |
| **Mouse** | Olhar |
| **Shift** | Correr |
| **Ctrl / C** | Agachar |
| **Space** | Pular |
| **Mouse Esquerdo** | Atirar |
| **Mouse Direito** | Mirar (ADS) |
| **R** | Recarregar |
| **E** | Interagir |
| **1-3** | Trocar arma |
| **Scroll** | Trocar arma |
| **Tab** | Leaderboard |
| **Esc** | Pausar |
| **Enter** | Chat |

---

## 🌐 Como Jogar Online

### Hospedar (Host)
1. No menu principal, insira seu nome
2. Clique em **"Host Game"**
3. Compartilhe seu **IP** com seus amigos

### Conectar (Join)
1. No menu principal, insira seu nome
2. Insira o **IP do host**
3. Clique em **"Join Game"**

### Lobby
1. Todos os jogadores devem clicar em **"Ready"**
2. Quando todos estiverem prontos, a contagem regressiva começa
3. O host pode forçar o início

---

## 📝 Sistemas Técnicos

### Arquitetura de Rede
- **Modelo Client-Server** usando Mirror
- **SyncVar** para sincronização de estado
- **Command/ClientRpc** para comunicação client-server
- **SyncList** para leaderboard sincronizado

### IA dos Zombies
- **Máquina de Estados**: Idle → Wandering → Chasing → Attacking
- **NavMesh**: Pathfinding com obstacle avoidance
- **Detecção**: FOV visual + detecção por audição
- **Tipos**: Comportamentos diferentes por tipo

### Otimização
- **Object Pooling**: Reutilização de objetos (balas, efeitos)
- **LOD**: Level of Detail para performance
- **Occlusion Culling**: Não renderizar objetos não visíveis
- **NavMesh Agents**: Pathfinding otimizado pela Unity

---

## 🔧 Configuração Avançada

### Servidor Dedicado
Para criar um servidor dedicado:
1. Build com `-batchmode -nographics`
2. Adicione lógica de auto-start no GameNetworkManager
3. Configure port forwarding na porta 7777 (padrão Mirror)

### Backend de Ranking (Produção)
O leaderboard atual usa PlayerPrefs (local). Para produção:
1. Implemente uma API REST (Node.js/Python)
2. Use Firebase, PlayFab, ou seu próprio backend
3. Substitua `SaveLeaderboard()`/`LoadLeaderboard()` por chamadas HTTP

---

## 📄 Licença

Este projeto é para uso educacional e pessoal.

---

## 👨‍💻 Desenvolvido com

- **Unity 2022.3 LTS**
- **C# 10**
- **Mirror Networking**
- **Universal Render Pipeline (URP)**
- **TextMeshPro**
- **NavMesh AI**
- **Post-Processing Stack**

---

*Feito com ❤️ por Bruno*
