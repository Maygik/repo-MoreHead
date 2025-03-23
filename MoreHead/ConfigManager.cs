using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json; // 使用 Newtonsoft.Json 库
using BepInEx.Logging;
using UnityEngine; // 添加UnityEngine引用，用于访问Application.persistentDataPath

namespace MoreHead
{
    // 配置管理器
    public static class ConfigManager
    {
        // 日志记录器
        private static ManualLogSource? Logger => Morehead.Logger;
        
        // MOD数据文件夹名称
        private const string MOD_DATA_FOLDER = "REPOModData";
        
        // MOD特定文件夹名称
        private const string MOD_FOLDER = "MoreHead";
        
        // 配置文件名称
        private const string CONFIG_FILENAME = "MoreHeadConfig.json";
        
        // 新的配置文件路径（Unity通用存档位置）
        private static string NewConfigFilePath => Path.Combine(
            Application.persistentDataPath, // Unity通用存档位置
            MOD_DATA_FOLDER,               // MOD数据总文件夹
            MOD_FOLDER,                    // 本MOD特定文件夹
            CONFIG_FILENAME                // 配置文件名
        );
        
        // 旧的BepInEx配置文件路径
        private static string BepInExConfigFilePath => Path.Combine(
            BepInEx.Paths.ConfigPath,      // BepInEx配置目录
            CONFIG_FILENAME                // 配置文件名
        );
        
        // 更旧的配置文件路径（用于迁移）
        private static string OldConfigFilePath => Path.Combine(
            Path.GetDirectoryName(Morehead.Instance?.Info.Location) ?? string.Empty,
            "MoreHeadConfig.txt"
        );
        
        // 装饰物状态字典
        private static Dictionary<string?, bool> _decorationStates = new Dictionary<string?, bool>();
        
        // 初始化配置管理器
        public static void Initialize()
        {
            try
            {
                // 确保MOD数据目录存在
                EnsureModDataDirectoryExists();
                
                // 加载配置
                LoadConfig();
                
                // 应用已保存的装饰物状态
                ApplySavedStates();
            }
            catch (Exception e)
            {
                Logger?.LogError($"初始化配置管理器时出错: {e.Message}");
            }
        }
        
        // 确保MOD数据目录存在
        private static void EnsureModDataDirectoryExists()
        {
            try
            {
                // 创建MOD数据总文件夹
                string modDataPath = Path.Combine(Application.persistentDataPath, MOD_DATA_FOLDER);
                if (!Directory.Exists(modDataPath))
                {
                    Directory.CreateDirectory(modDataPath);
                    Logger?.LogInfo($"已创建MOD数据总文件夹: {modDataPath}");
                }
                
                // 创建本MOD特定文件夹
                string modFolderPath = Path.Combine(modDataPath, MOD_FOLDER);
                if (!Directory.Exists(modFolderPath))
                {
                    Directory.CreateDirectory(modFolderPath);
                    Logger?.LogInfo($"已创建MOD特定文件夹: {modFolderPath}");
                }
            }
            catch (Exception e)
            {
                Logger?.LogError($"创建MOD数据目录时出错: {e.Message}");
            }
        }
        
        // 加载配置
        private static void LoadConfig()
        {
            try
            {
                // 清空状态字典
                _decorationStates.Clear();
                
                // 首先尝试从新的Unity存档位置加载
                if (File.Exists(NewConfigFilePath))
                {
                    if (LoadJsonConfig(NewConfigFilePath))
                    {
                        Logger?.LogInfo($"已从Unity存档位置加载配置: {NewConfigFilePath}");
                        return; // 成功加载，直接返回
                    }
                }
                
                // 如果从新位置加载失败，尝试从BepInEx配置目录加载并迁移
                if (File.Exists(BepInExConfigFilePath))
                {
                    if (LoadJsonConfig(BepInExConfigFilePath))
                    {
                        Logger?.LogInfo($"已从BepInEx配置目录加载配置: {BepInExConfigFilePath}");
                        
                        // 立即保存到新位置
                        SaveConfigWithoutUpdate();
                        Logger?.LogInfo($"已将配置从BepInEx目录迁移到Unity存档位置: {NewConfigFilePath}");
                        
                        // 尝试删除旧配置文件（可选）
                        try
                        {
                            File.Delete(BepInExConfigFilePath);
                            Logger?.LogInfo($"已删除BepInEx配置文件: {BepInExConfigFilePath}");
                        }
                        catch (Exception ex)
                        {
                            // 删除失败不影响主程序，只记录日志
                            Logger?.LogWarning($"删除BepInEx配置文件失败: {ex.Message}");
                        }
                        
                        return; // 成功加载并迁移，直接返回
                    }
                }
                
                // 如果前两种方式都失败，尝试从最旧的位置加载文本配置并迁移
                if (File.Exists(OldConfigFilePath))
                {
                    try
                    {
                        // 读取所有行
                        string[] lines = File.ReadAllLines(OldConfigFilePath);
                        
                        // 解析每一行
                        foreach (string line in lines)
                        {
                            // 跳过空行
                            if (string.IsNullOrWhiteSpace(line))
                                continue;
                            
                            // 分割行内容
                            string[] parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                string? name = parts[0].Trim();
                                bool isVisible = parts[1].Trim().Equals("1", StringComparison.OrdinalIgnoreCase);
                                
                                // 添加到状态字典
                                _decorationStates[name] = isVisible;
                            }
                        }
                        
                        if (_decorationStates.Count > 0)
                        {
                            Logger?.LogInfo($"已从旧文本格式加载配置，包含 {_decorationStates.Count} 个装饰物状态");
                            
                            // 立即保存为新格式到新位置
                            SaveConfigWithoutUpdate();
                            Logger?.LogInfo($"已将旧文本格式配置迁移到新的JSON格式: {NewConfigFilePath}");
                            
                            // 尝试删除旧配置文件
                            try
                            {
                                File.Delete(OldConfigFilePath);
                                Logger?.LogInfo($"已删除旧文本配置文件: {OldConfigFilePath}");
                            }
                            catch (Exception ex)
                            {
                                // 删除失败不影响主程序，只记录日志
                                Logger?.LogWarning($"删除旧文本配置文件失败: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError($"从旧文本格式加载配置时出错: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Logger?.LogError($"加载配置时出错: {e.Message}");
                
                // 清空状态字典
                _decorationStates.Clear();
            }
        }
        
        // 从JSON文件加载配置
        private static bool LoadJsonConfig(string filePath)
        {
            try
            {
                // 读取JSON文件内容
                string jsonContent = File.ReadAllText(filePath);
                
                // 反序列化JSON到字典
                var loadedStates = JsonConvert.DeserializeObject<Dictionary<string, bool>>(jsonContent);
                
                // 将加载的状态添加到字典
                if (loadedStates != null)
                {
                    foreach (var kvp in loadedStates)
                    {
                        _decorationStates[kvp.Key] = kvp.Value;
                    }
                    
                    Logger?.LogInfo($"已从JSON加载配置，包含 {_decorationStates.Count} 个装饰物状态");
                    return true; // 成功加载
                }
            }
            catch (JsonException je)
            {
                Logger?.LogError($"解析JSON配置文件时出错: {je.Message}");
            }
            catch (Exception e)
            {
                Logger?.LogError($"加载JSON配置文件时出错: {e.Message}");
            }
            
            return false; // 加载失败
        }
        
        // 保存配置
        public static void SaveConfig()
        {
            try
            {
                // 更新配置数据
                UpdateConfigData();
                
                // 保存到文件
                SaveToFile();
            }
            catch (Exception e)
            {
                Logger?.LogError($"保存配置时出错: {e.Message}");
            }
        }
        
        // 保存配置但不更新数据（用于迁移）
        private static void SaveConfigWithoutUpdate()
        {
            try
            {
                // 直接保存当前状态字典，不更新数据
                SaveToFile();
            }
            catch (Exception e)
            {
                Logger?.LogError($"保存配置时出错: {e.Message}");
            }
        }
        
        // 保存到文件
        private static void SaveToFile()
        {
            try
            {
                // 确保配置目录存在
                EnsureModDataDirectoryExists();
                
                // 序列化为JSON，使用格式化输出提高可读性
                string jsonContent = JsonConvert.SerializeObject(_decorationStates, Formatting.Indented);
                
                // 写入配置文件
                File.WriteAllText(NewConfigFilePath, jsonContent);
                
                //Logger?.LogInfo($"已保存配置到 {NewConfigFilePath}");
            }
            catch (Exception e)
            {
                Logger?.LogError($"写入配置文件时出错: {e.Message}");
            }
        }
        
        // 更新配置数据
        private static void UpdateConfigData()
        {
            // 清空装饰物状态
            _decorationStates.Clear();
            
            // 添加当前装饰物状态
            foreach (var decoration in HeadDecorationManager.Decorations)
            {
                _decorationStates[decoration.Name] = decoration.IsVisible;
            }
        }
        
        // 应用已保存的装饰物状态
        public static void ApplySavedStates()
        {
            try
            {
                int appliedCount = 0;
                
                // 遍历所有装饰物
                foreach (var decoration in HeadDecorationManager.Decorations)
                {
                    // 检查是否有保存的状态
                    if (_decorationStates.TryGetValue(decoration.Name, out bool isVisible))
                    {
                        // 应用保存的状态
                        decoration.IsVisible = isVisible;
                        appliedCount++;
                    }
                }
                
                if (appliedCount > 0)
                {
                    Logger?.LogInfo($"已应用 {appliedCount} 个已保存的装饰物状态");
                }
            }
            catch (Exception e)
            {
                Logger?.LogError($"应用已保存的装饰物状态时出错: {e.Message}");
            }
        }
    }
} 