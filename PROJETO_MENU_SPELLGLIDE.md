# SpellGlide - Documentação Geral do Projeto & Contexto para IA

Este documento reúne a visão completa, arquitetura e histórico de desenvolvimento de **SpellGlide**, servindo como guia de contexto para desenvolvedores e IAs continuarem o projeto sem perder histórico ou diretrizes de design.

---

## 🧭 Visão Geral do Jogo

- **Título:** SpellGlide
- **Gênero:** 2D Side-scrolling Shoot 'em Up (Shmup) com elementos de RPG / Roguelite.
- **Temática:** Fantasia mágica. O jogador controla uma bruxa que voa montada em uma vassoura mágica através de masmorras sombrias, conjurando magias elementais (fogo e gelo), coletando essências, evoluindo habilidades e enfrentando chefes épicos (como o Golem de Pedra).
- **Engine:** Unity 6 (6000.0.x) - 2D URP.

---

## 🚩 Marcos de Desenvolvimento: Game Jam vs. Versão Definitiva

> [!IMPORTANT]
> **Atenção para qualquer IA ou Desenvolvedor lendo este projeto:**
> - **Fase Game Jam (Concluída):** Tudo o que foi desenvolvido até o momento corresponde ao protótipo criado para a **SENAI GameJamV**. O escopo foi concebido sob restrições de tempo, com foco em entregar um loop de gameplay completo, menu inicial, história em quadrinhos introdutória e mecânicas básicas funcionais.
> - **Fase Atual (Versão de Polimento & Definitiva):** A partir deste ponto, **todas as alterações, refatorações e novas features solicitadas fazem parte do polimento e evolução para a versão definitiva do jogo** (nível comercial/lançamento). Deve-se priorizar game feel, acabamento visual e sonoro, estabilidade, balanceamento, refinamento de arquitetura e qualidade geral.

---

## 🕹️ O que foi Desenvolvido na Fase da Game Jam

### 1. Menu Principal & Interface (`Assets/Scripts/MainMenuManager.cs`)
- **Telas e Navegação:**
  - Botões principais: `Jogar`, `Opções`, `Sair` e botão de `Resetar Dados`.
  - Painel de Opções com controle deslizante de volume para Música (BGM) e Efeitos Sonoros (SFX).
  - Tabela explicativa de controles no painel de opções.
  - Botão de reset de progresso integrado diretamente ao menu com feedback sonoro e limpeza de dados.
- **Arte de Fundo:** Utiliza arte oficial estilizada (`Assets/Menu e HQ'S/Menu SpellGlide.png`).

### 2. Prólogo / Cutscene em Quadrinhos (`Assets/Scripts/ComicIntroManager.cs`)
- Sistema narrativo sequencial exibindo 6 painéis de quadrinhos (`Assets/Menu e HQ'S/1.png` a `6.png`).
- Transições com **Fade In / Fade Out** em CanvasGroup.
- Caixa de texto com efeito dinâmico de **Máquina de Escrever (Typewriter)** e som a cada caractere.
- **Sistema de Pulo Inteligente (Skip):**
  - Primeiro toque: Completa a frase imediatamente.
  - Segundo toque: Pula o quadrinho atual e avança para o próximo.
- Carregamento automático da cena de gameplay ao final da história.

### 3. Loop Central de Gameplay & Personagem
- **Movimentação da Bruxa (`PlayerController.cs`):** Movimentação 2D multidirecional em vassoura mágica (WASD / Setas).
- **Sistema de Combate (`PlayerShooting.cs`, `Projectile.cs`, `ElementType.cs`):**
  - Alternância entre magias de Fogo e Gelo.
  - Bônus de perfuração, cadência de tiro, dano aumentado e multishot.
- **Vida e Sobrevivência (`PlayerHealth.cs`):**
  - Sistema de corações / barra de vida, invulnerabilidade temporária pós-dano, redução de dano (Pele de Pedra) e habilidade de Segundo Fôlego (reviver).
  - Uso de poções de cura estocadas no inventário via tecla `F`.

### 4. Inimigos, Spawn e Chefe
- **Inimigos Normais:**
  - `BatEnemy.cs`: Morcegos voadores com ataque de projéteis de fogo.
  - `RusherEnemy.cs`: Inimigos rápidos com foco em investidas diretas de dano de contato.
  - `WaveSpawner.cs`: Ondas dinâmicas de inimigos com cadência configurável.
- **Boss Golem de Pedra (`GolemBoss.cs`):**
  - Cutscene de introdução dedicada (`BossIntroCutscene.cs`).
  - Ataque de arremesso de grandes rochas (`BigRockProjectile.cs`).
  - Barreira de rochas / obstáculo vertical dinâmico (`StoneWallObstacle.cs`).
  - Pedras orbitais de proteção (`OrbitingStone.cs`).

### 5. Árvore de Talentos & Evolução do Cajado (`EvolutionUI.cs`, `GameManager.cs`)
- **Tela de Upgrades (Tecla `E`):**
  - Atributos evolutivos: Vitalidade, Velocidade, Foco Arcano, Despertar, Pele de Pedra, Ímã de Almas, Segundo Fôlego, Tiro Rápido, Tiro Duplo e Tiro Perfurante.
- **Evolução do Cajado (Tecla `T`):**
  - Requer Corações de Fogo e Núcleos de Boss para desbloquear poderes amplificados no cajado.

### 6. Sistema de Save & Persistência (`SaveSystem.cs`, `PlayModeSaveCleaner.cs`)
- Arquivo de save em disco JSON (`witch_save.json`) em `Application.persistentDataPath` com espelho em `PlayerPrefs`.
- **Comportamento refinado de limpeza:**
  - **No Unity Editor:** O script editor `PlayModeSaveCleaner.cs` zera o save sempre que o desenvolvedor entra no Play Mode para garantir testes limpos.
  - **Na Build:** O sistema detecta se é uma nova compilação (`Application.buildGUID`) e zera os dados na primeira execução, preservando o save entre fechamentos e aberturas subsequentes da mesma versão.
  - **Reset Manual:** Botão "Resetar Dados" no menu principal limpa arquivos e PlayerPrefs imediatamente.

### 7. Sistema de Áudio & Síntese Procedural
- `AudioManager.cs`: Gerenciador de SFX e canais de áudio no menu.
- `BGMManager.cs`: Gerenciador de trilhas sonoras em loop para dungeon e chefe.
- `ProceduralAudioGenerator.cs`: Gerador nativo em C# para síntese de efeitos WAV matematicamente sintetizados (tiros de fogo/gelo, impactos, cliques, tema do menu, ataques de boss).
- Automação via scripts PowerShell (`gerar_audios.ps1`) para geração e validação de áudios sem dependências externas.

---

## 🎮 Mapeamento Padrão de Controles

| Ação | Tecla / Comando |
|---|---|
| **Mover Bruxa** | `W, A, S, D` ou `Setas Direcionais` |
| **Atirar / Conjurar Magia** | `Mouse 1` (Botão Esquerdo) |
| **Usar Poção de Vida** | `F` |
| **Abrir/Fechar Árvore de Habilidades** | `E` |
| **Evoluir Cajado** | `T` |
| **Pausar Jogo** | `ESC` |
| **Pular Quadrinhos / Texto** | Qualquer tecla / Clique |

---

## 🛠️ Diretrizes para a Fase de Polimento (Versão Definitiva)

Ao propor ou executar qualquer modificação a partir de agora:

1. **Foco em Game Feel & Juice:** Efeitos de partículas, screen shake controlado, feedback de impacto nos inimigos, transições suaves de áudio e UI polida.
2. **Preservação de Compatibilidade:** Manter referências de cenas, componentes de UI e dados do `SaveSystem` intactos, evitando quebrar ligações no Inspector do Unity.
3. **Consistência Arquitetural:**
   - Scripts do núcleo de gameplay utilizam o namespace `WitchShmup.Core`, `WitchShmup.Player`, `WitchShmup.UI`, etc.
   - Scripts de interface e menu utilizam o namespace `SpellGlide`.
4. **Sem Perda de Contexto:** Não remover mecânicas existentes sem alinhamento explícito com o usuário. Sempre construir sobre a base sólida desenvolvida na Game Jam.
