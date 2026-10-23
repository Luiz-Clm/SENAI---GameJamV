using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace SpellGlide.Editor
{
    [InitializeOnLoad]
    public static class MenuSetupHelper
    {
        static MenuSetupHelper()
        {
            EditorApplication.delayCall += AutoInitialize;
        }

        private static void AutoInitialize()
        {
            EnsureAudioFilesExist();
            string menuScenePath = Path.Combine(Application.dataPath, "Scenes/Menu.unity");
            if (!Application.isPlaying && !File.Exists(menuScenePath))
            {
                CreateCompleteMenuScene(false);
            }
        }

        [MenuItem("Tools/SpellGlide/1. Gerar/Atualizar Arquivos de Áudio (WAV)")]
        public static void GenerateAudioFilesFromMenu()
        {
            EnsureAudioFilesExist(forceRegenerate: true, showDialog: true);
        }

        public static void EnsureAudioFilesExist(bool forceRegenerate = false, bool showDialog = false)
        {
            string audioDir = Path.Combine(Application.dataPath, "Audio");
            if (!Directory.Exists(audioDir))
            {
                Directory.CreateDirectory(audioDir);
            }

            string musicPath = Path.Combine(audioDir, "MenuMusic.wav");
            string clickPath = Path.Combine(audioDir, "ButtonClick.wav");
            string typewriterPath = Path.Combine(audioDir, "TypewriterClick.wav");
            string skipPath = Path.Combine(audioDir, "SkipSound.wav");
            string fireShotPath = Path.Combine(audioDir, "PlayerFireShot.wav");
            string iceShotPath = Path.Combine(audioDir, "PlayerIceShot.wav");
            string batFireballPath = Path.Combine(audioDir, "BatFireball.wav");
            string bossRockPath = Path.Combine(audioDir, "BossRockThrow.wav");
            string bossWallPath = Path.Combine(audioDir, "BossStoneWall.wav");

            bool createdAny = false;

            if (forceRegenerate || !File.Exists(musicPath))
            {
                AudioClip clip = ProceduralAudioGenerator.CreateMenuMusic();
                File.WriteAllBytes(musicPath, ProceduralAudioGenerator.ConvertClipToWav(clip));
                createdAny = true;
            }

            if (forceRegenerate || !File.Exists(clickPath))
            {
                AudioClip clip = ProceduralAudioGenerator.CreateClickSound();
                File.WriteAllBytes(clickPath, ProceduralAudioGenerator.ConvertClipToWav(clip));
                createdAny = true;
            }

            if (forceRegenerate || !File.Exists(typewriterPath))
            {
                AudioClip clip = ProceduralAudioGenerator.CreateTypewriterSound();
                File.WriteAllBytes(typewriterPath, ProceduralAudioGenerator.ConvertClipToWav(clip));
                createdAny = true;
            }

            if (forceRegenerate || !File.Exists(skipPath))
            {
                AudioClip clip = ProceduralAudioGenerator.CreateSkipSound();
                File.WriteAllBytes(skipPath, ProceduralAudioGenerator.ConvertClipToWav(clip));
                createdAny = true;
            }

            if (forceRegenerate || !File.Exists(fireShotPath))
            {
                AudioClip clip = ProceduralAudioGenerator.CreatePlayerFireShotSound();
                File.WriteAllBytes(fireShotPath, ProceduralAudioGenerator.ConvertClipToWav(clip));
                createdAny = true;
            }

            if (forceRegenerate || !File.Exists(iceShotPath))
            {
                AudioClip clip = ProceduralAudioGenerator.CreatePlayerIceShotSound();
                File.WriteAllBytes(iceShotPath, ProceduralAudioGenerator.ConvertClipToWav(clip));
                createdAny = true;
            }

            if (forceRegenerate || !File.Exists(batFireballPath))
            {
                AudioClip clip = ProceduralAudioGenerator.CreateBatFireballSound();
                File.WriteAllBytes(batFireballPath, ProceduralAudioGenerator.ConvertClipToWav(clip));
                createdAny = true;
            }

            if (forceRegenerate || !File.Exists(bossRockPath))
            {
                AudioClip clip = ProceduralAudioGenerator.CreateBossRockThrowSound();
                File.WriteAllBytes(bossRockPath, ProceduralAudioGenerator.ConvertClipToWav(clip));
                createdAny = true;
            }

            if (forceRegenerate || !File.Exists(bossWallPath))
            {
                AudioClip clip = ProceduralAudioGenerator.CreateBossStoneWallSound();
                File.WriteAllBytes(bossWallPath, ProceduralAudioGenerator.ConvertClipToWav(clip));
                createdAny = true;
            }

            if (createdAny)
            {
                AssetDatabase.Refresh();
                Debug.Log("[SpellGlide] Arquivos de áudio (.wav) gerados com sucesso em Assets/Audio/!");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog("SpellGlide - Áudios Gerados!",
                    "Todos os 9 arquivos de áudio (.wav) foram gerados com sucesso em Assets/Audio/:\n\n" +
                    "• PlayerFireShot.wav (Tiro de Fogo do Jogador)\n" +
                    "• PlayerIceShot.wav (Tiro de Gelo do Jogador)\n" +
                    "• BatFireball.wav (Tiro do Morcego de Fogo)\n" +
                    "• BossRockThrow.wav (Ataque de Rocha do Boss)\n" +
                    "• BossStoneWall.wav (Parede de Pedra do Boss)\n" +
                    "• MenuMusic.wav, ButtonClick.wav, TypewriterClick.wav, SkipSound.wav",
                    "Sensacional!");
            }
        }

        [MenuItem("Tools/SpellGlide/2. Criar Cena de Menu e HQ Completa")]
        public static void CreateCompleteMenuSceneFromMenu()
        {
            CreateCompleteMenuScene(true);
        }

        [MenuItem("Tools/SpellGlide/3. Atualizar Painel de Opções na Cena Aberta")]
        public static void UpdateOptionsPanelInOpenScene()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("SpellGlide", "Nenhum Canvas encontrado na cena aberta atualmente!", "OK");
                return;
            }

            Transform existingPanel = canvas.transform.Find("OptionsPanel");
            int siblingIndex = -1;
            if (existingPanel != null)
            {
                siblingIndex = existingPanel.GetSiblingIndex();
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            GameObject optionsPanelObj = BuildOptionsPanel(canvas.gameObject, out Slider musicSlider, out Slider sfxSlider, out Button closeOptBtn);
            if (siblingIndex >= 0)
            {
                optionsPanelObj.transform.SetSiblingIndex(siblingIndex);
            }

            MainMenuManager menuMgr = Object.FindFirstObjectByType<MainMenuManager>();
            if (menuMgr != null)
            {
                SerializedObject menuSO = new SerializedObject(menuMgr);
                menuSO.FindProperty("optionsPanel").objectReferenceValue = optionsPanelObj;
                menuSO.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;
                menuSO.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
                menuSO.FindProperty("closeOptionsButton").objectReferenceValue = closeOptBtn;
                menuSO.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("SpellGlide", "Painel de Opções atualizado com sucesso com os novos controles na cena!", "Ótimo!");
        }

        [MenuItem("Tools/SpellGlide/4. Restaurar Fundo Padrão do Narrador na Cena Aberta")]
        public static void RestoreDefaultNarratorBoxInOpenScene()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("SpellGlide", "Nenhum Canvas encontrado na cena aberta atualmente!", "OK");
                return;
            }

            Transform textBoxTr = canvas.transform.Find("ComicIntroPanel/ComicContentGroup/TextBoxBackdrop");
            if (textBoxTr == null)
            {
                foreach (var img in canvas.GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject.name == "TextBoxBackdrop")
                    {
                        textBoxTr = img.transform;
                        break;
                    }
                }
            }

            if (textBoxTr == null)
            {
                EditorUtility.DisplayDialog("SpellGlide", "Objeto 'TextBoxBackdrop' não encontrado na cena!", "OK");
                return;
            }

            Image textBoxImg = textBoxTr.GetComponent<Image>();
            textBoxImg.sprite = null;
            textBoxImg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

            Transform storyTextTr = textBoxTr.Find("StoryText");
            if (storyTextTr != null)
            {
                RectTransform storyRt = storyTextTr.GetComponent<RectTransform>();
                storyRt.anchorMin = new Vector2(0.03f, 0.1f);
                storyRt.anchorMax = new Vector2(0.97f, 0.9f);
                storyRt.offsetMin = Vector2.zero;
                storyRt.offsetMax = Vector2.zero;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("SpellGlide", "O fundo padrão e original da caixa do narrador foi restaurado com sucesso!", "Perfeito!");
        }

        public static void CreateCompleteMenuScene(bool showDialog = false)
        {
            EnsureAudioFilesExist();

            // Salva se necessário e cria nova cena
            Scene currentScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Câmera Principal
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            // 2. Event System (Necessário para a UI)
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            #if ENABLE_INPUT_SYSTEM
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            #else
            eventSystemObj.AddComponent<StandaloneInputModule>();
            #endif

            // 3. Canvas Principal
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 4. AudioManager
            GameObject audioManagerObj = new GameObject("AudioManager");
            AudioManager audioMgr = audioManagerObj.AddComponent<AudioManager>();

            AudioClip musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/MenuMusic.wav");
            AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ButtonClick.wav");
            AudioClip typewriterClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/TypewriterClick.wav");
            AudioClip skipClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SkipSound.wav");

            SerializedObject audioSO = new SerializedObject(audioMgr);
            audioSO.FindProperty("menuMusic").objectReferenceValue = musicClip;
            audioSO.FindProperty("buttonClickClip").objectReferenceValue = clickClip;
            audioSO.FindProperty("typewriterClip").objectReferenceValue = typewriterClip;
            audioSO.FindProperty("skipClip").objectReferenceValue = skipClip;
            audioSO.ApplyModifiedProperties();

            // ----------------------------------------------------
            // 5. PAINEL DO MENU PRINCIPAL
            // ----------------------------------------------------
            GameObject menuPanelObj = new GameObject("MenuPanel", typeof(RectTransform));
            menuPanelObj.transform.SetParent(canvasObj.transform, false);
            StretchFull(menuPanelObj.GetComponent<RectTransform>());

            // Imagem de fundo do Menu SpellGlide
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(menuPanelObj.transform, false);
            StretchFull(bgObj.GetComponent<RectTransform>());
            Image bgImg = bgObj.GetComponent<Image>();

            // Carrega sprite do Menu SpellGlide
            Sprite menuBgSprite = LoadSprite("Assets/Menu e HQ'S/Menu SpellGlide.png");
            if (menuBgSprite != null)
            {
                bgImg.sprite = menuBgSprite;
            }

            // Container dos Botões na posição exata da arte (canto inferior esquerdo)
            GameObject buttonsContainer = new GameObject("ButtonsContainer", typeof(RectTransform));
            buttonsContainer.transform.SetParent(menuPanelObj.transform, false);
            RectTransform btnContRect = buttonsContainer.GetComponent<RectTransform>();
            btnContRect.anchorMin = new Vector2(0f, 0f);
            btnContRect.anchorMax = new Vector2(0.35f, 0.55f);
            btnContRect.pivot = new Vector2(0f, 0f);
            btnContRect.anchoredPosition = new Vector2(100f, 150f);
            btnContRect.sizeDelta = new Vector2(400f, 300f);

            // Cria os 3 botões estilizados
            Button playBtn = CreateMenuButton(buttonsContainer.transform, "JogarButton", "Jogar", new Vector2(0, 100));
            Button optionsBtn = CreateMenuButton(buttonsContainer.transform, "OpcoesButton", "Opções", new Vector2(0, 20));
            Button quitBtn = CreateMenuButton(buttonsContainer.transform, "SairButton", "Sair", new Vector2(0, -60));

            // Botão Resetar Dados (solto no centro da tela, para o dev reposicionar depois)
            Button resetBtn = CreateMenuButton(menuPanelObj.transform, "ResetDataButton", "Resetar Dados", new Vector2(0, 0));
            // Estiliza com cor vermelha para indicar ação destrutiva
            Image resetImg = resetBtn.GetComponent<Image>();
            resetImg.color = new Color(0.6f, 0.15f, 0.15f, 0.92f);
            ColorBlock resetCb = resetBtn.colors;
            resetCb.normalColor = new Color(0.6f, 0.15f, 0.15f, 0.95f);
            resetCb.highlightedColor = new Color(0.75f, 0.2f, 0.2f, 1f);
            resetCb.pressedColor = new Color(0.4f, 0.1f, 0.1f, 1f);
            resetBtn.colors = resetCb;
            // Texto do botão em branco
            TextMeshProUGUI resetTMP = resetBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (resetTMP != null) resetTMP.color = Color.white;

            // ----------------------------------------------------
            // 6. PAINEL DE OPÇÕES (Áudio e Controles do Jogo)
            // ----------------------------------------------------
            GameObject optionsPanelObj = BuildOptionsPanel(canvasObj, out Slider musicSlider, out Slider sfxSlider, out Button closeOptBtn);

            // ----------------------------------------------------
            // 7. PAINEL DA HISTÓRIA / HQ (CUTSCENE)
            // ----------------------------------------------------
            GameObject comicPanelObj = new GameObject("ComicIntroPanel", typeof(RectTransform));
            comicPanelObj.transform.SetParent(canvasObj.transform, false);
            StretchFull(comicPanelObj.GetComponent<RectTransform>());

            // Fundo preto puro da HQ
            GameObject comicBgObj = new GameObject("BlackBackdrop", typeof(RectTransform), typeof(Image));
            comicBgObj.transform.SetParent(comicPanelObj.transform, false);
            StretchFull(comicBgObj.GetComponent<RectTransform>());
            comicBgObj.GetComponent<Image>().color = Color.black;

            // Grupo com CanvasGroup para Fade In / Fade Out do conteúdo
            GameObject comicContentObj = new GameObject("ComicContentGroup", typeof(RectTransform), typeof(CanvasGroup));
            comicContentObj.transform.SetParent(comicPanelObj.transform, false);
            StretchFull(comicContentObj.GetComponent<RectTransform>());
            CanvasGroup comicCG = comicContentObj.GetComponent<CanvasGroup>();
            comicCG.alpha = 0f;

            // Imagem do Quadrinho (HQ)
            GameObject comicImageObj = new GameObject("ComicImage", typeof(RectTransform), typeof(Image));
            comicImageObj.transform.SetParent(comicContentObj.transform, false);
            RectTransform comicImgRect = comicImageObj.GetComponent<RectTransform>();
            comicImgRect.anchorMin = new Vector2(0.1f, 0.22f);
            comicImgRect.anchorMax = new Vector2(0.9f, 0.95f);
            comicImgRect.offsetMin = Vector2.zero;
            comicImgRect.offsetMax = Vector2.zero;
            Image comicDisplayImg = comicImageObj.GetComponent<Image>();
            comicDisplayImg.preserveAspect = true;

            // Caixa de texto da narração na parte de baixo
            GameObject textBoxObj = new GameObject("TextBoxBackdrop", typeof(RectTransform), typeof(Image));
            textBoxObj.transform.SetParent(comicContentObj.transform, false);
            RectTransform textBoxRect = textBoxObj.GetComponent<RectTransform>();
            textBoxRect.anchorMin = new Vector2(0.08f, 0.04f);
            textBoxRect.anchorMax = new Vector2(0.92f, 0.20f);
            textBoxRect.offsetMin = Vector2.zero;
            textBoxRect.offsetMax = Vector2.zero;
            Image textBoxBg = textBoxObj.GetComponent<Image>();
            textBoxBg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

            // Texto narrativo (Typewriter)
            GameObject storyTextObj = new GameObject("StoryText", typeof(RectTransform), typeof(TextMeshProUGUI));
            storyTextObj.transform.SetParent(textBoxObj.transform, false);
            RectTransform storyTextRect = storyTextObj.GetComponent<RectTransform>();
            storyTextRect.anchorMin = new Vector2(0.03f, 0.1f);
            storyTextRect.anchorMax = new Vector2(0.97f, 0.9f);
            storyTextRect.offsetMin = Vector2.zero;
            storyTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI storyTMP = storyTextObj.GetComponent<TextMeshProUGUI>();
            storyTMP.fontSize = 28;
            storyTMP.color = Color.white;
            storyTMP.alignment = TextAlignmentOptions.MidlineLeft;
            storyTMP.enableWordWrapping = true;
            storyTMP.text = "Coloque seu texto aqui";

            // Texto de ajuda para avançar entre os quadrinhos
            GameObject skipTextObj = new GameObject("SkipPromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            skipTextObj.transform.SetParent(comicPanelObj.transform, false);
            RectTransform skipRect = skipTextObj.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.6f, 0.005f);
            skipRect.anchorMax = new Vector2(0.98f, 0.045f);
            skipRect.offsetMin = Vector2.zero;
            skipRect.offsetMax = Vector2.zero;
            TextMeshProUGUI skipTMP = skipTextObj.GetComponent<TextMeshProUGUI>();
            skipTMP.fontSize = 20;
            skipTMP.color = new Color(0.85f, 0.85f, 0.9f, 0.75f);
            skipTMP.alignment = TextAlignmentOptions.MidlineRight;
            skipTMP.text = "Clique ou aperte uma tecla para avançar";

            // Screen Fader (para transição final para a cena do jogo)
            GameObject faderObj = new GameObject("ScreenFader", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            faderObj.transform.SetParent(canvasObj.transform, false);
            StretchFull(faderObj.GetComponent<RectTransform>());
            faderObj.GetComponent<Image>().color = Color.black;
            CanvasGroup faderCG = faderObj.GetComponent<CanvasGroup>();
            faderCG.alpha = 0f;
            faderCG.blocksRaycasts = false;

            comicPanelObj.SetActive(false);

            // ----------------------------------------------------
            // 8. CONFIGURAÇÃO DO COMIC INTRO MANAGER
            // ----------------------------------------------------
            GameObject introMgrObj = new GameObject("ComicIntroManager");
            ComicIntroManager introMgr = introMgrObj.AddComponent<ComicIntroManager>();

            SerializedObject introSO = new SerializedObject(introMgr);
            introSO.FindProperty("comicRootPanel").objectReferenceValue = comicPanelObj;
            introSO.FindProperty("comicImage").objectReferenceValue = comicDisplayImg;
            introSO.FindProperty("comicContentCanvasGroup").objectReferenceValue = comicCG;
            introSO.FindProperty("storyTmpText").objectReferenceValue = storyTMP;
            introSO.FindProperty("skipPromptTmpText").objectReferenceValue = skipTMP;
            introSO.FindProperty("screenFader").objectReferenceValue = faderCG;
            introSO.FindProperty("audioManager").objectReferenceValue = audioMgr;
            introSO.FindProperty("nextSceneName").stringValue = "Fase";
            introSO.FindProperty("panelDisplayDuration").floatValue = 10.0f;

            // Carrega os 6 sprites dos quadrinhos
            SerializedProperty spritesProp = introSO.FindProperty("comicSprites");
            spritesProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                Sprite s = LoadSprite($"Assets/Menu e HQ'S/{i + 1}.png");
                spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = s;
            }

            // Textos padrão "Coloque seu texto aqui"
            SerializedProperty textsProp = introSO.FindProperty("comicTexts");
            textsProp.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                textsProp.GetArrayElementAtIndex(i).stringValue = "Coloque seu texto aqui";
            }
            introSO.ApplyModifiedProperties();

            // ----------------------------------------------------
            // 9. CONFIGURAÇÃO DO MAIN MENU MANAGER
            // ----------------------------------------------------
            GameObject menuMgrObj = new GameObject("MainMenuManager");
            MainMenuManager menuMgr = menuMgrObj.AddComponent<MainMenuManager>();

            SerializedObject menuSO = new SerializedObject(menuMgr);
            menuSO.FindProperty("menuRootPanel").objectReferenceValue = menuPanelObj;
            menuSO.FindProperty("optionsPanel").objectReferenceValue = optionsPanelObj;
            menuSO.FindProperty("playButton").objectReferenceValue = playBtn;
            menuSO.FindProperty("optionsButton").objectReferenceValue = optionsBtn;
            menuSO.FindProperty("quitButton").objectReferenceValue = quitBtn;
            menuSO.FindProperty("resetDataButton").objectReferenceValue = resetBtn;
            menuSO.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;
            menuSO.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
            menuSO.FindProperty("closeOptionsButton").objectReferenceValue = closeOptBtn;
            menuSO.FindProperty("comicIntroManager").objectReferenceValue = introMgr;
            menuSO.FindProperty("audioManager").objectReferenceValue = audioMgr;
            menuSO.ApplyModifiedProperties();

            // 10. Salva a cena em Assets/Scenes/Menu.unity
            string scenesDir = Path.Combine(Application.dataPath, "Scenes");
            if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);

            string scenePath = "Assets/Scenes/Menu.unity";
            EditorSceneManager.SaveScene(currentScene, scenePath);

            // 11. Registra as cenas no EditorBuildSettings (Menu = 0, Fase = 1)
            UpdateBuildSettings(scenePath);

            Debug.Log($"[SpellGlide] Cena de Menu criada e salva com sucesso em {scenePath}!");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("SpellGlide - Menu Criado!",
                    "A cena do Menu com os 3 botões (Jogar, Opções, Sair), Painel de Opções (Áudio + Controles do Jogo) e os 6 quadrinhos com Fade In/Out, efeito Typewriter, áudio e pulo foi criada com sucesso em:\nAssets/Scenes/Menu.unity\n\nBasta abrir a cena e pressionar Play para testar!",
                    "Excelente!");
            }
        }

        private static void UpdateBuildSettings(string menuScenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(menuScenePath, true));

            string faseScenePath = "Assets/Scenes/Fase.unity";
            if (File.Exists(Path.Combine(Application.dataPath, "Scenes/Fase.unity")))
            {
                scenes.Add(new EditorBuildSettingsScene(faseScenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null) return sprite;

            var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in all)
            {
                if (obj is Sprite s) return s;
            }
            return null;
        }

        private static Button CreateMenuButton(Transform parent, string name, string labelText, Vector2 pos)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280f, 54f);
            rt.anchoredPosition = pos;

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(0.95f, 0.95f, 0.95f, 0.92f);

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 0.95f);
            cb.highlightedColor = new Color(0.9f, 0.95f, 1f, 1f);
            cb.pressedColor = new Color(0.7f, 0.8f, 0.95f, 1f);
            btn.colors = cb;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            StretchFull(textRt);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.12f, 0.12f, 0.15f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        public static GameObject BuildOptionsPanel(GameObject canvasObj, out Slider musicSlider, out Slider sfxSlider, out Button closeOptBtn)
        {
            GameObject optionsPanelObj = new GameObject("OptionsPanel", typeof(RectTransform), typeof(Image));
            optionsPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform optRect = optionsPanelObj.GetComponent<RectTransform>();
            optRect.sizeDelta = new Vector2(650f, 620f);
            optRect.anchoredPosition = Vector2.zero;
            Image optBg = optionsPanelObj.GetComponent<Image>();
            optBg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);

            // Título Opções
            CreateLabel(optionsPanelObj.transform, "Titulo", "OPÇÕES", 34, new Vector2(0, 255));

            // --- Seção Áudio ---
            CreateLabel(optionsPanelObj.transform, "AudioHeader", "ÁUDIO", 18, new Vector2(0, 205), new Color(0.45f, 0.8f, 1f, 1f));

            // Slider Música
            CreateLabel(optionsPanelObj.transform, "MusicaLabel", "Volume da Música", 18, new Vector2(-120, 160), Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(240f, 35f));
            musicSlider = CreateSimpleSlider(optionsPanelObj.transform, "MusicSlider", new Vector2(120, 160));

            // Slider SFX
            CreateLabel(optionsPanelObj.transform, "SFXLabel", "Volume dos Efeitos", 18, new Vector2(-120, 115), Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(240f, 35f));
            sfxSlider = CreateSimpleSlider(optionsPanelObj.transform, "SFXSlider", new Vector2(120, 115));

            // --- Seção Controles ---
            GameObject controlsBox = new GameObject("ControlsBox", typeof(RectTransform), typeof(Image));
            controlsBox.transform.SetParent(optionsPanelObj.transform, false);
            RectTransform ctrlBoxRect = controlsBox.GetComponent<RectTransform>();
            ctrlBoxRect.sizeDelta = new Vector2(560f, 230f);
            ctrlBoxRect.anchoredPosition = new Vector2(0f, -45f);
            Image ctrlBoxImg = controlsBox.GetComponent<Image>();
            ctrlBoxImg.color = new Color(0.04f, 0.04f, 0.07f, 0.85f);

            CreateLabel(controlsBox.transform, "ControlesHeader", "CONTROLES DO JOGO", 18, new Vector2(0, 80), new Color(0.45f, 0.8f, 1f, 1f));

            CreateControlRow(controlsBox.transform, "Mover", "SETAS / WASD", 35f);
            CreateControlRow(controlsBox.transform, "Atirar", "M1", -5f);
            CreateControlRow(controlsBox.transform, "Árvore de Upgrades", "E", -45f);
            CreateControlRow(controlsBox.transform, "Evoluir Cajado", "T", -85f);

            // Botão Voltar
            closeOptBtn = CreateMenuButton(optionsPanelObj.transform, "VoltarButton", "Voltar", new Vector2(0, -245));
            closeOptBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 48f);

            optionsPanelObj.SetActive(false);
            return optionsPanelObj;
        }

        private static void CreateControlRow(Transform parent, string actionName, string keyName, float yPos)
        {
            string cleanId = actionName.Replace(" ", "").Replace("Á", "A").Replace("á", "a");

            // Ação (ex: "Mover:", "Atirar:")
            CreateLabel(parent, cleanId + "Label", actionName + ":", 18, new Vector2(-120f, yPos),
                new Color(0.85f, 0.85f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft, new Vector2(240f, 32f));

            // Tecla / Atalho (ex: "[ SETAS / WASD ]", "[ M1 ]")
            CreateLabel(parent, cleanId + "Key", $"[ {keyName} ]", 18, new Vector2(120f, yPos),
                new Color(1f, 0.86f, 0.35f, 1f), TextAlignmentOptions.MidlineRight, new Vector2(240f, 32f));
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, Vector2 pos, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Center, Vector2? sizeDelta = null)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = sizeDelta ?? new Vector2(300f, 50f);
            rt.anchoredPosition = pos;

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color ?? Color.white;
            tmp.alignment = alignment;
            return tmp;
        }

        private static Slider CreateSimpleSlider(Transform parent, string name, Vector2 pos)
        {
            GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);
            RectTransform rt = sliderObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 24f);
            rt.anchoredPosition = pos;

            // Background
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderObj.transform, false);
            StretchFull(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRt.offsetMin = Vector2.zero;
            fillAreaRt.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            StretchFull(fill.GetComponent<RectTransform>());
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.3f, 0.65f, 1f, 1f);

            Slider slider = sliderObj.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;

            return slider;
        }
    }
}
