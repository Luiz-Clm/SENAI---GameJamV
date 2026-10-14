# Contexto do Projeto - Menu e História (SpellGlide)

Este arquivo serve para sincronizar o contexto do projeto entre computadores e com o Antigravity.

---

## 📌 O que foi feito até agora:

1. **Menu Principal (`Assets/Scripts/MainMenuManager.cs`)**:
   - 3 Botões: `Jogar`, `Opções`, `Sair`.
   - Fundo com a arte oficial de `Assets/Menu e HQ'S/Menu SpellGlide.png`.
   - Janela/Painel de Opções com controle de volume para Música e Efeitos Sonoros (SFX) e botão de Voltar.
   - Botão Sair fecha o jogo (e para o editor da Unity).

2. **Leitor de HQs / História Intro (`Assets/Scripts/ComicIntroManager.cs`)**:
   - 6 quadrinhos usando as imagens em `Assets/Menu e HQ'S/1.png` até `6.png`.
   - **Fade In** da imagem do quadrinho ao iniciar.
   - **Caixa de narração** inferior com **efeito Máquina de Escrever (Typewriter)**.
   - Cada quadrinho tem texto padrão configurável: `"Coloque seu texto aqui"` (editável no Inspector).
   - **Tempo de 10 segundos** de exibição antes de fazer **Fade Out** e ir para o próximo quadrinho.
   - Ao terminar o 6º quadrinho, faz transição e carrega a cena do jogo (`Fase.unity`).

3. **Sistema de Pulo ("Aperte qualquer tecla para pular")**:
   - Texto de aviso no canto inferior direito.
   - **1º Aperto de tecla/clique**: Pula a animação do texto e completa a frase na hora.
   - **2º Aperto de tecla/clique** (com texto completo): Pula os 10s e avança imediatamente para o próximo quadrinho.
   - Toca som de pulo em ambas as ações.

4. **Sistema de Áudio (`Assets/Scripts/AudioManager.cs` e `ProceduralAudioGenerator.cs`)**:
   - Música tema do menu em loop (gerada em `Assets/Audio/MenuMusic.wav`).
   - Som de clique nos botões (`ButtonClick.wav`).
   - Som de máquina de escrever a cada caractere digitado (`TypewriterClick.wav`).
   - Som ao pular/avançar tela (`SkipSound.wav`).
   - Fallback procedural caso os arquivos não estejam atribuídos.

5. **Ferramenta de Criação Automática no Unity (`Assets/Scripts/Editor/MenuSetupHelper.cs`)**:
   - Menu no topo do Unity: `Tools -> SpellGlide -> 2. Criar Cena de Menu e HQ Completa`.
   - Monta a cena inteira com 1 clique e salva em `Assets/Scenes/Menu.unity`.

---

## 🚀 Como continuar o trabalho em um novo computador:
1. Abra o projeto clonado do GitHub no Unity.
2. Abra o chat do Antigravity e mencione este arquivo digitando:
   `@PROJETO_MENU_SPELLGLIDE.md Vamos continuar de onde paramos!`
