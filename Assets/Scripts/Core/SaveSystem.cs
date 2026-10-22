using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WitchShmup.Core
{
    [Serializable]
    public class SavedSkillLevel
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class SaveData
    {
        [Header("Progresso das Fases")]
        public int currentPhase = 1;
        public int maxUnlockedPhase = 1;
        public bool hasSeenStory = false;

        [Header("Pontuação")]
        public int currentScore = 0;
        public int highScore = 0;

        [Header("Materiais e Moedas")]
        public int skillPoints = 2;
        public int fireHeartsCount = 0;
        public int bossCoresCount = 0;
        public int healthPotionsInInventory = 0;

        [Header("Evolução do Cajado")]
        public bool isStaffEvolved = false;

        [Header("Níveis da Árvore de Habilidades")]
        public int levelVit = 0;  // Vitalidade
        public int levelVel = 0;  // Vassoura Veloz
        public int levelFoc = 0;  // Foco Arcano
        public int levelDes = 0;  // Despertar Arcano
        public int levelPel = 0;  // Pele de Pedra
        public int levelIma = 0;  // Ímã de Almas
        public int levelSeg = 0;  // Segundo Fôlego
        public int levelRap = 0;  // Tiro Rápido
        public int levelDup = 0;  // Tiro Duplo
        public int levelPer = 0;  // Tiro Perfurante
        public List<SavedSkillLevel> customSkillLevels = new List<SavedSkillLevel>();

        public int GetSkillLevel(string id)
        {
            switch (id)
            {
                case "vit": return levelVit;
                case "vel": return levelVel;
                case "foc": return levelFoc;
                case "des": return levelDes;
                case "pel": return levelPel;
                case "ima": return levelIma;
                case "seg": return levelSeg;
                case "rap": return levelRap;
                case "dup": return levelDup;
                case "per": return levelPer;
                default: return 0;
            }
        }

        public void SetSkillLevel(string id, int level)
        {
            switch (id)
            {
                case "vit": levelVit = level; break;
                case "vel": levelVel = level; break;
                case "foc": levelFoc = level; break;
                case "des": levelDes = level; break;
                case "pel": levelPel = level; break;
                case "ima": levelIma = level; break;
                case "seg": levelSeg = level; break;
                case "rap": levelRap = level; break;
                case "dup": levelDup = level; break;
                case "per": levelPer = level; break;
            }
        }
    }

    public static class SaveSystem
    {
        private const string PREFS_KEY = "WitchShmup_SaveData_v1";
        private const string SELECTED_PHASE_KEY = "WitchShmup_SelectedPhase";
        private const string BUILD_ID_KEY = "WitchShmup_BuildID";
        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, "witch_save.json");

        /// <summary>
        /// Deve ser chamado uma vez no início do jogo (ex: primeira cena carregada).
        /// Detecta se é a primeira execução de uma nova build e limpa dados antigos.
        /// No Editor, o PlayModeSaveCleaner já cuida disso, então aqui só atua em builds.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClearSaveIfNewBuild()
        {
#if UNITY_EDITOR
            // No Editor, o PlayModeSaveCleaner.cs já limpa os dados.
            // Não fazemos nada aqui para evitar limpar duas vezes.
            return;
#else
            // Em builds: verifica se o ID da build mudou (nova instalação/build)
            string currentBuildId = Application.buildGUID;

            // Se o buildGUID estiver vazio (versões antigas do Unity), usa version como fallback
            if (string.IsNullOrEmpty(currentBuildId))
            {
                currentBuildId = Application.version;
            }

            string storedBuildId = PlayerPrefs.GetString(BUILD_ID_KEY, "");

            if (storedBuildId != currentBuildId)
            {
                // Primeira execução desta build! Limpa dados antigos.
                Debug.Log($"[SaveSystem] Nova build detectada (anterior: '{storedBuildId}', atual: '{currentBuildId}'). Limpando dados salvos.");
                DeleteSave();
                PlayerPrefs.DeleteKey("WitchShmup_HighScore");

                // Salva o ID da build atual para que na próxima execução os dados persistam
                PlayerPrefs.SetString(BUILD_ID_KEY, currentBuildId);
                PlayerPrefs.Save();
            }
#endif
        }

        /// <summary>
        /// Reseta TODOS os dados salvos do jogo (save, highscore, build ID).
        /// Chamado pelo botão "Resetar Dados" no menu.
        /// </summary>
        public static void ResetAllData()
        {
            DeleteSave();
            PlayerPrefs.DeleteKey("WitchShmup_HighScore");
            // Mantém o BUILD_ID_KEY para não re-limpar na próxima abertura
            PlayerPrefs.Save();
            Debug.Log("[SaveSystem] Todos os dados do jogo foram resetados pelo jogador.");
        }

        public static void SaveGame(SaveData data)
        {
            if (data == null) return;

            try
            {
                string json = JsonUtility.ToJson(data, true);

                // Salva em arquivo no disco
                File.WriteAllText(SaveFilePath, json);

                // Backup de segurança em PlayerPrefs
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.SetInt(SELECTED_PHASE_KEY, data.currentPhase);
                PlayerPrefs.Save();

                Debug.Log($"[SaveSystem] Jogo salvo com sucesso! Fase: {data.currentPhase}, SP: {data.skillPoints}, Cajado: {data.isStaffEvolved}, Caminho: {SaveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Erro ao salvar jogo: {ex.Message}");
            }
        }

        public static SaveData LoadGame()
        {
            SaveData data = null;

            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        data = JsonUtility.FromJson<SaveData>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Falha ao ler arquivo de save, tentando PlayerPrefs: {ex.Message}");
            }

            // Fallback para PlayerPrefs se o arquivo não existir ou falhar
            if (data == null && PlayerPrefs.HasKey(PREFS_KEY))
            {
                try
                {
                    string prefsJson = PlayerPrefs.GetString(PREFS_KEY, "");
                    if (!string.IsNullOrEmpty(prefsJson))
                    {
                        data = JsonUtility.FromJson<SaveData>(prefsJson);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveSystem] Erro ao carregar PlayerPrefs: {ex.Message}");
                }
            }

            if (data == null)
            {
                data = new SaveData();
                Debug.Log("[SaveSystem] Nenhum save encontrado. Criando novos dados padrão.");
            }
            else
            {
                Debug.Log($"[SaveSystem] Save carregado com sucesso! Fase: {data.currentPhase}, SP: {data.skillPoints}, Cajado Evoluído: {data.isStaffEvolved}");
            }

            return data;
        }

        public static bool HasSave()
        {
            return File.Exists(SaveFilePath) || PlayerPrefs.HasKey(PREFS_KEY);
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveSystem] Erro ao deletar arquivo de save: {ex.Message}");
            }

            PlayerPrefs.DeleteKey(PREFS_KEY);
            PlayerPrefs.DeleteKey(SELECTED_PHASE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[SaveSystem] Save deletado.");
        }

        public static void SetSelectedPhase(int phase)
        {
            PlayerPrefs.SetInt(SELECTED_PHASE_KEY, phase);
            PlayerPrefs.Save();
        }

        public static int GetSelectedPhase()
        {
            return PlayerPrefs.GetInt(SELECTED_PHASE_KEY, 1);
        }

        public static int GetMaxUnlockedPhase()
        {
            var data = LoadGame();
            return Mathf.Max(1, data.maxUnlockedPhase);
        }

        public static void UnlockPhase(int phase)
        {
            var data = LoadGame();
            if (phase > data.maxUnlockedPhase)
            {
                data.maxUnlockedPhase = phase;
                SaveGame(data);
            }
        }

        public static bool HasSeenStory()
        {
            var data = LoadGame();
            return data != null && (data.hasSeenStory || data.maxUnlockedPhase > 1);
        }

        public static void SetStorySeen(bool seen = true)
        {
            var data = LoadGame();
            data.hasSeenStory = seen;
            SaveGame(data);
        }
    }
}
