using System;
using BepInEx.Logging;
using MenuLib;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System.IO;
using MenuLib.MonoBehaviors;
using MenuLib.Structs;

namespace MoreHead
{
    // MoreHead的UI管理类
    public static class MoreHeadUI
    {
        // 日志记录器
        private static ManualLogSource? Logger => Morehead.Logger;
        
        // UI元素
        private static REPOButton? menuButton;
        private static REPOPopupPage? decorationsPage;
        
        // 装饰物按钮字典
        private static Dictionary<string?, REPOButton> decorationButtons = new Dictionary<string?, REPOButton>();
        
        // 标签筛选器
        private static string currentTagFilter = "ALL";
        private static Dictionary<string, REPOButton> tagFilterButtons = new Dictionary<string, REPOButton>();
        
        // 装饰物数据缓存 - 存储所有标签的装饰物数据
        private static Dictionary<string, List<DecorationInfo>> decorationDataCache = new Dictionary<string, List<DecorationInfo>>();
        
        // 按钮数据缓存 - 存储按钮文本和状态，避免重复计算
        private static Dictionary<string, Dictionary<string, string>> buttonTextCache = new Dictionary<string, Dictionary<string, string>>();
        
        // 按钮和页面名称常量
        private const string BUTTON_NAME = "<color=#FF0000>M</color><color=#FF3300>O</color><color=#FF6600>R</color><color=#FF9900>E</color><color=#FFCC00>H</color><color=#FFDD00>E</color><color=#FFEE00>A</color><color=#FFFF00>D</color>";
        private const string PAGE_TITLE = "Rotate robot: A/D";
        
        // 所有可用标签
        private static readonly string[] ALL_TAGS = new string[] { "ALL", "HEAD", "NECK", "BODY", "HIP", "WORLD" };

        // 初始化UI
        public static void Initialize()
        {
            try
            {
                // 创建ESC菜单按钮
                MenuAPI.AddElementToEscapeMenu(parent => {
                    menuButton = MenuAPI.CreateREPOButton(BUTTON_NAME, OnMenuButtonClick, parent, Vector2.zero);
                });
                
                // 初始化数据缓存
                InitializeDataCache();
                
                Logger?.LogInfo("MoreHead UI已初始化");
            }
            catch (Exception e)
            {
                Logger?.LogError($"初始化UI时出错: {e.Message}");
            }
        }
        
        // 初始化数据缓存
        private static void InitializeDataCache()
        {
            try
            {
                // 清空现有缓存
                decorationDataCache.Clear();
                buttonTextCache.Clear();
                
                // 为每个标签创建数据缓存
                foreach (string tag in ALL_TAGS)
                {
                    // 筛选出属于该标签的装饰物
                    var filteredDecorations = HeadDecorationManager.Decorations
                        .Where(decoration => tag == "ALL" || (decoration.ParentTag?.ToLower() == tag.ToLower()))
                        .ToList();
                    
                    // 添加到缓存
                    decorationDataCache[tag] = filteredDecorations;
                    
                    // 为每个标签创建按钮文本缓存
                    buttonTextCache[tag] = new Dictionary<string, string>();
                    
                    // 预先计算并缓存所有按钮文本
                    foreach (var decoration in filteredDecorations)
                    {
                        string buttonText = GetButtonText(decoration, decoration.IsVisible);
                        buttonTextCache[tag][decoration.Name ?? string.Empty] = buttonText;
                    }
                }
                
                Logger?.LogInfo($"数据缓存初始化完成，共缓存了 {ALL_TAGS.Length} 个标签的装饰物数据");
            }
            catch (Exception e)
            {
                Logger?.LogError($"初始化数据缓存时出错: {e.Message}");
            }
        }
        
        // 主菜单按钮点击事件
        private static void OnMenuButtonClick()
        {
            try
            {
                // 创建新页面
                decorationsPage = MenuAPI.CreateREPOPopupPage(PAGE_TITLE, true, 0, new Vector2(-200, 140));
                
                // 设置页面属性
                SetupPopupPage(decorationsPage);
                
                // 使用缓存的数据创建页面内容
                CreatePageContentWithCache(decorationsPage, currentTagFilter);
                
                // 添加作者标记
                AddAuthorCredit(decorationsPage);
                
                // 打开页面
                decorationsPage.OpenPage(false);
                
                // 移动玩家模型到前面
                MovePlayerAvatarToFront();
                
                // 更新所有按钮状态
                UpdateButtonStates();
            }
            catch (Exception e)
            {
                Logger?.LogError($"打开设置页面时出错: {e.Message}");
            }
        }
        
        // 设置弹出页面属性
        private static void SetupPopupPage(REPOPopupPage page)
        {
            try
            {
                // 设置页面名称
                if (page.gameObject != null)
                {
                    page.gameObject.name = $"MoreHead_Page_{currentTagFilter}";
                }
                
                // 设置页面大小和位置
                page.rectTransform.sizeDelta = new Vector2(300f, 350f);
                page.pageDimmerVisibility = true;
                page.maskPadding = new Padding(10f, 10f, 20f, 10f);
                page.headerTMP.rectTransform.position = new Vector3(170, 344, 0);
            }
            catch (Exception e)
            {
                Logger?.LogError($"设置弹出页面属性时出错: {e.Message}");
            }
        }
        
        // 添加作者标记
        private static void AddAuthorCredit(REPOPopupPage page)
        {
            try
            {
                // 创建作者标记按钮（使用按钮作为文本显示，但不添加点击事件）
                page.AddElement(parent => {
                    var authorCredit = MenuAPI.CreateREPOButton("<size=10><color=#FFFFA0>Masaicker</color> and <color=#FFFFA0>Yuriscat</color> co-developed.\n由<color=#FFFFA0>马赛克了</color>和<color=#FFFFA0>尤里的猫</color>共同制作。</size>", () => {}, parent, new Vector2(300, 329));
                    
                    // 尝试获取按钮组件并修改其外观
                    try
                    {
                        // 等待一帧以确保按钮已经创建
                        UnityEngine.MonoBehaviour.FindObjectOfType<MenuManager>()?.StartCoroutine(DelayedStyleAuthorCredit(authorCredit));
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogWarning($"无法修改作者标记样式: {ex.Message}");
                    }
                });
            }
            catch (Exception e)
            {
                Logger?.LogError($"添加作者标记时出错: {e.Message}");
            }
        }
        
        // 延迟修改作者标记样式
        private static System.Collections.IEnumerator DelayedStyleAuthorCredit(REPOButton button)
        {
            // 等待一帧
            yield return null;
            
            try
            {
                // 查找按钮对象 - 使用更通用的方式查找
                GameObject? buttonObj = null;
                
                // 查找所有按钮
                var allButtons = GameObject.FindObjectsOfType<Button>();
                foreach (var btn in allButtons)
                {
                    // 检查按钮名称是否包含作者信息
                    if (btn.name.Contains("Masaicker") && btn.name.Contains("Yuriscat"))
                    {
                        buttonObj = btn.gameObject;
                        break;
                    }
                }
                
                if (buttonObj != null)
                {
                    // 获取按钮组件
                    var buttonComponent = buttonObj.GetComponent<Button>();
                    if (buttonComponent != null)
                    {
                        // 禁用按钮交互
                        buttonComponent.interactable = false;
                        
                        // 移除按钮背景
                        var images = buttonObj.GetComponentsInChildren<Image>();
                        foreach (var image in images)
                        {
                            if (image.gameObject != buttonComponent.gameObject)
                            {
                                image.enabled = false;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger?.LogWarning($"修改作者标记样式失败: {e.Message}");
            }
        }
        
        // 使用缓存的数据创建页面内容
        private static void CreatePageContentWithCache(REPOPopupPage? page, string tag)
        {
            try
            {
                // 清空当前页面的按钮字典
                decorationButtons.Clear();
                
                // 获取缓存的装饰物数据
                if (!decorationDataCache.TryGetValue(tag, out var decorations))
                {
                    Logger?.LogWarning($"找不到标签 {tag} 的装饰物数据缓存");
                    // 回退到直接查询装饰物
                    decorations = HeadDecorationManager.Decorations
                        .Where(decoration => tag == "ALL" || (decoration.ParentTag?.ToLower() == tag.ToLower()))
                        .ToList();
                }
                
                // 将装饰物分为内置模型和外部模型两组
                var builtInDecorations = decorations
                    .Where(decoration => IsBuiltInDecoration(decoration))
                    .OrderBy(decoration => decoration.DisplayName)
                    .ToList();
                
                var externalDecorations = decorations
                    .Where(decoration => !IsBuiltInDecoration(decoration))
                    .OrderBy(decoration => decoration.DisplayName)
                    .ToList();
                
                // 首先添加内置模型按钮
                foreach (var decoration in builtInDecorations)
                {
                    // 获取缓存的按钮文本
                    string buttonText;
                    if (!buttonTextCache.TryGetValue(tag, out var textCache) || 
                        !textCache.TryGetValue(decoration.Name ?? string.Empty, out buttonText))
                    {
                        // 如果没有缓存，计算按钮文本
                        buttonText = GetButtonText(decoration, decoration.IsVisible);
                        
                        // 更新缓存
                        if (buttonTextCache.TryGetValue(tag, out textCache))
                        {
                            textCache[decoration.Name ?? string.Empty] = buttonText;
                        }
                    }
                
                    // 使用新版API添加按钮到滚动视图
                    page?.AddElementToScrollView(scrollView => {
                        var button = MenuAPI.CreateREPOButton(
                            buttonText, 
                            () => OnDecorationButtonClick(decoration.Name),
                            scrollView,
                            new Vector2(0, 0) // Y位置会被滚动视图自动调整
                        );
                        
                        // 添加到当前页面的按钮字典
                        decorationButtons[decoration.Name ?? string.Empty] = button;
                        
                        return button.rectTransform;
                    }, topPadding: 2, bottomPadding: -20);
                }
                
                // 然后添加外部模型按钮
                foreach (var decoration in externalDecorations)
                {
                    // 获取缓存的按钮文本
                    string buttonText;
                    if (!buttonTextCache.TryGetValue(tag, out var textCache) || 
                        !textCache.TryGetValue(decoration.Name ?? string.Empty, out buttonText))
                    {
                        // 如果没有缓存，计算按钮文本
                        buttonText = GetButtonText(decoration, decoration.IsVisible);
                        
                        // 更新缓存
                        if (buttonTextCache.TryGetValue(tag, out textCache))
                        {
                            textCache[decoration.Name ?? string.Empty] = buttonText;
                        }
                    }
                    
                    // 使用新版API添加按钮到滚动视图
                    page?.AddElementToScrollView(scrollView => {
                        var button = MenuAPI.CreateREPOButton(
                            buttonText, 
                            () => OnDecorationButtonClick(decoration.Name),
                            scrollView,
                            new Vector2(0, 0) // Y位置会被滚动视图自动调整
                        );
                        
                        // 添加到当前页面的按钮字典
                        decorationButtons[decoration.Name ?? string.Empty] = button;
                        
                        return button.rectTransform;
                    }, topPadding: 2, bottomPadding: -20);
                }
                
                // 创建标签筛选按钮
                CreateTagFilterButtons(page);
                
                // 创建关闭按钮 - 放在页面底部，不在滚动区域内
                page?.AddElement(parent => {
                    MenuAPI.CreateREPOButton(
                        "<size=18><color=#FFFFFF>C</color><color=#E6E6E6>L</color><color=#CCCCCC>O</color><color=#B3B3B3>S</color><color=#999999>E</color></size>", 
                        OnCloseButtonClick, 
                        parent, 
                        new Vector2(301, 0)
                    );
                });
                
                // 创建"关闭所有模型"按钮 - 放在关闭按钮旁边，使用橙黄色底色和黑色文字
                page?.AddElement(parent => {
                    MenuAPI.CreateREPOButton(
                        "<size=18><color=#FFAA00>CLEAR ALL</color></size>", 
                        OnDisableAllButtonClick, 
                        parent, 
                        new Vector2(401, 0)
                    );
                });
            }
            catch (Exception e)
            {
                Logger?.LogError($"创建页面内容时出错: {e.Message}");
            }
        }
        
        // 创建页面内容 - 在初始化和切换标签时调用
        private static void CreatePageContent()
        {
            // 使用当前标签创建页面内容
            CreatePageContentWithCache(decorationsPage, currentTagFilter);
        }
        
        // 为特定标签创建页面内容
        private static void CreatePageContentForTag(REPOPopupPage? page, string tag)
        {
            // 为了向后兼容，调用新的方法
            CreatePageContentWithCache(page, tag);
        }
        
        // 判断是否为内置模型
        private static bool IsBuiltInDecoration(DecorationInfo decoration)
        {
            // 内置模型的源路径中会包含MOD的主要目录路径下的Decorations文件夹
            // 而不是外部DLL中的资源
            string? bundlePath = decoration.BundlePath;
            if (string.IsNullOrEmpty(bundlePath))
                return false;
                
            // 获取MOD所在目录和装饰物目录
            string? modDirectory = Path.GetDirectoryName(Morehead.Instance?.Info.Location);
            if (string.IsNullOrEmpty(modDirectory))
                return false;
                
            string decorationsDirectory = Path.Combine(modDirectory, "Decorations");
            
            // 判断路径是否在MOD的Decorations目录下
            return bundlePath.StartsWith(decorationsDirectory);
        }
        
        // 创建标签筛选按钮
        private static void CreateTagFilterButtons(REPOPopupPage? page)
        {
            try
            {
                // 清空标签按钮字典
                tagFilterButtons.Clear();
                
                // 标签按钮的水平间距
                const int buttonSpacing = 40; // 减小间距，使按钮靠近一点
                // 起始X坐标
                const int startX = 70;
                // Y坐标
                const int y = 20; // 向上移动，避免与其他按钮重叠
                
                // 为每个标签创建按钮
                for (int i = 0; i < ALL_TAGS.Length; i++)
                {
                    string tag = ALL_TAGS[i];
                    string lowerTag = tag.ToLower();
                    int index = i; // 捕获循环变量
                    
                    // 标签颜色（与GetButtonText方法中的颜色保持一致）
                    string tagColor = lowerTag switch
                    {
                        "head" => "#00AAFF", // 蓝色
                        "neck" => "#AA00FF", // 紫色
                        "body" => "#FFAA00", // 橙色
                        "hip" => "#FF00AA", // 粉色
                        "world" => "#00FFAA", // 青色
                        _ => "#FFFFFF"       // 白色（ALL标签）
                    };
                    
                    // 如果是当前选中的标签，则使用更亮的颜色和加粗效果
                    string buttonText = lowerTag == currentTagFilter.ToLower() ?
                        $"<size=14><b><color={tagColor}>{tag}</color></b></size>" :
                        $"<size=14><color={tagColor}50>{tag}</color></size>";
                    
                    // 创建按钮 - 确保标签大小写一致
                    string tagForCallback = lowerTag == "all" ? "ALL" : tag;
                    
                    // 计算按钮位置
                    int xPosition = startX + i * buttonSpacing;
                    
                    page?.AddElement(parent => {
                        var button = MenuAPI.CreateREPOButton(
                            buttonText, 
                            () => OnTagFilterButtonClick(tagForCallback), 
                            parent, 
                            new Vector2(xPosition, y)
                        );
                        
                        // 添加到标签按钮字典
                        tagFilterButtons[tagForCallback] = button;
                    });
                }
            }
            catch (Exception e)
            {
                Logger?.LogError($"创建标签筛选按钮时出错: {e.Message}");
            }
        }
        
        // 标签筛选按钮点击事件
        private static void OnTagFilterButtonClick(string tag)
        {
            try
            {
                // 如果点击的是当前标签，不做任何操作
                if (tag == currentTagFilter)
                {
                    return;
                }
                
                // 关闭当前页面
                if (decorationsPage != null)
                {
                    decorationsPage.ClosePage(true);
                    decorationsPage = null;
                }
                
                // 更新当前标签筛选器
                currentTagFilter = tag;
                
                // 使用数据缓存创建新页面
                CreateTagPage(tag);
            }
            catch (Exception e)
            {
                Logger?.LogError($"切换标签筛选时出错: {e.Message}");
            }
        }
        
        // 创建标签页面的方法
        private static void CreateTagPage(string tag)
        {
            try
            {
                // 创建该标签的页面
                decorationsPage = MenuAPI.CreateREPOPopupPage(PAGE_TITLE, true, 0, new Vector2(-200, 140));
                
                // 设置页面属性
                SetupPopupPage(decorationsPage);
                
                // 使用缓存的数据创建页面内容
                CreatePageContentWithCache(decorationsPage, tag);
                
                // 添加作者标记
                AddAuthorCredit(decorationsPage);
                
                // 打开页面
                decorationsPage.OpenPage(false);
                
                // 移动玩家模型到前面
                MovePlayerAvatarToFront();
                
                // 更新所有按钮状态
                UpdateButtonStates();
            }
            catch (Exception e)
            {
                Logger?.LogError($"创建标签页面时出错: {e.Message}");
            }
        }
        
        // 更新所有按钮状态
        private static void UpdateButtonStates()
        {
            try
            {
                // 如果按钮字典为空，不需要更新
                if (decorationButtons.Count == 0)
                {
                    return;
                }
                
                // 获取当前标签的装饰物
                if (decorationDataCache.TryGetValue(currentTagFilter, out var decorations))
                {
                    // 更新每个装饰物按钮
                    foreach (var decoration in decorations)
                    {
                        if (decorationButtons.TryGetValue(decoration.Name ?? string.Empty, out REPOButton button))
                        {
                            // 获取缓存的按钮文本或重新计算
                            string buttonText;
                            if (buttonTextCache.TryGetValue(currentTagFilter, out var textCache) && 
                                textCache.TryGetValue(decoration.Name ?? string.Empty, out buttonText))
                            {
                                // 使用缓存的按钮文本
                            }
                            else
                            {
                                // 重新计算按钮文本
                                buttonText = GetButtonText(decoration, decoration.IsVisible);
                                
                                // 更新缓存
                                if (buttonTextCache.TryGetValue(currentTagFilter, out textCache))
                                {
                                    textCache[decoration.Name ?? string.Empty] = buttonText;
                                }
                            }
                            
                            button.labelTMP.text = buttonText;
                        }
                    }
                }
                
                // 更新标签按钮高亮状态
                UpdateTagButtonHighlights();
            }
            catch (Exception e)
            {
                Logger?.LogError($"更新按钮状态时出错: {e.Message}");
            }
        }
        
        // 更新标签按钮高亮状态
        private static void UpdateTagButtonHighlights()
        {
            try
            {
                // 更新每个标签按钮的高亮状态
                foreach (string tag in ALL_TAGS)
                {
                    // 确保标签格式一致
                    string tagKey = tag == "ALL" ? "ALL" : tag;
                    
                    if (tagFilterButtons.TryGetValue(tagKey, out REPOButton button))
                    {
                        // 标签颜色
                        string tagColor = tag.ToLower() switch
                        {
                            "head" => "#00AAFF", // 蓝色
                            "neck" => "#AA00FF", // 紫色
                            "body" => "#FFAA00", // 橙色
                            "hip" => "#FF00AA", // 粉色
                            "world" => "#00FFAA", // 青色
                            _ => "#FFFFFF"       // 白色（ALL标签）
                        };
                        
                        // 如果是当前选中的标签，则使用更亮的颜色和加粗效果
                        string buttonText = tagKey == currentTagFilter ?
                            $"<size=14><b><color={tagColor}>{tag}</color></b></size>" :
                            $"<size=14><color={tagColor}50>{tag}</color></size>";
                        
                        button.labelTMP.text = buttonText;
                    }
                }
            }
            catch (Exception e)
            {
                Logger?.LogError($"更新标签按钮高亮状态时出错: {e.Message}");
            }
        }
        
        // 装饰物按钮点击事件
        private static void OnDecorationButtonClick(string? decorationName)
        {
            try
            {
                // 查找装饰物信息
                var decoration = HeadDecorationManager.Decorations.FirstOrDefault(d => d.Name != null && d.Name.Equals(decorationName, StringComparison.OrdinalIgnoreCase));
                if (decoration == null)
                {
                    Logger?.LogWarning($"OnDecorationButtonClick: 找不到装饰物: {decorationName}");
                    return;
                }
                
                // 切换装饰物状态
                bool newState = HeadDecorationManager.ToggleDecorationState(decorationName);
                
                // 更新按钮文本缓存
                string newButtonText = GetButtonText(decoration, newState);
                
                // 更新所有标签的按钮文本缓存
                foreach (string tag in ALL_TAGS)
                {
                    bool shouldShow = tag == "ALL" || 
                                     (decoration.ParentTag?.ToLower() == tag.ToLower());
                    
                    if (shouldShow && buttonTextCache.TryGetValue(tag, out var textCache))
                    {
                        textCache[decorationName ?? string.Empty] = newButtonText;
                    }
                }
                
                // 更新当前页面上的按钮状态
                if (decorationButtons.TryGetValue(decorationName ?? string.Empty, out REPOButton button))
                {
                    // 更新按钮文本以反映新状态
                    button.labelTMP.text = newButtonText;
                }
                
                // 更新玩家装饰物
                UpdateDecorations();
                
                // 保存配置
                ConfigManager.SaveConfig();
            }
            catch (Exception e)
            {
                Logger?.LogError($"切换装饰物 {decorationName} 状态时出错: {e.Message}");
            }
        }
        
        // 获取按钮文本
        private static string GetButtonText(DecorationInfo decoration, bool isEnabled)
        {
            // 获取装饰物名称和标签
            string name = decoration.DisplayName?.ToUpper() ?? "UNKNOWN";
            string parentTag = decoration.ParentTag ?? "unknown";
            
            // 为不同标签设置不同颜色（避免使用红色和绿色，因为已用于ON/OFF）
            string tagColor = parentTag.ToLower() switch
            {
                "head" => "#00AAFF", // 蓝色
                "neck" => "#AA00FF", // 紫色
                "body" => "#FFAA00", // 橙色
                "hip" => "#FF00AA", // 粉色
                "world" => "#00FFAA", // 青色
                _ => "#AAAAAA"       // 灰色（未知标签）
            };
            
            // 返回格式化的按钮文本
            return $"<size=16>{(isEnabled ? "<color=#00FF00>[+]</color>" : "<color=#FF0000>[-]</color>")} <color={tagColor}><size=12>({parentTag})</size></color> {name}</size>";
        }
        
        // 关闭按钮点击事件
        private static void OnCloseButtonClick()
        {
            try
            {
                // 关闭所有页面，模拟按下ESC键的效果
                MenuManager.instance.PageCloseAll();
            }
            catch (Exception e)
            {
                Logger?.LogError($"关闭页面时出错: {e.Message}");
            }
        }
        
        // 更新所有装饰物状态
        private static void UpdateDecorations()
        {
            try
            {
                // 查找本地玩家
                var localPlayer = FindLocalPlayer();
                if (localPlayer != null)
                {
                    // 更新本地玩家的装饰物状态
                    PlayerUpdatePatch.UpdatePlayerDecorations(localPlayer);
                    
                    // 通过RPC同步到其他玩家
                    var syncComponent = localPlayer.GetComponent<HeadDecorationSync>();
                    if (syncComponent != null)
                    {
                        // 使用新方法同步所有装饰物
                        syncComponent.SyncAllDecorations();
                    }
                }
                
                // 更新菜单角色的装饰物状态
                PlayerUpdatePatch.UpdateMenuPlayerDecorations();
            }
            catch (Exception e)
            {
                Logger?.LogError($"更新装饰物状态时出错: {e.Message}");
            }
        }
        
        // 查找本地玩家
        private static PlayerAvatar? FindLocalPlayer()
        {
            try
            {
                // 查找所有PlayerAvatar对象
                PlayerAvatar?[]? playerAvatars = UnityEngine.Object.FindObjectsOfType<PlayerAvatar>();
                foreach (var avatar in playerAvatars)
                {
                    // 检查是否是本地玩家
                    if (avatar?.photonView != null && avatar.photonView.IsMine)
                    {
                        return avatar;
                    }
                }
            }
            catch (Exception e)
            {
                Logger?.LogError($"查找本地玩家时出错: {e.Message}");
            }
            
            return null;
        }
        
        // 移动玩家模型到前面，防止被遮挡
        private static void MovePlayerAvatarToFront()
        {
            try
            {
                // 延迟执行，确保UI已经创建完成
                UnityEngine.GameObject.FindObjectOfType<UnityEngine.MonoBehaviour>()?.StartCoroutine(
                    DelayedMovePlayerAvatarToFront());
            }
            catch (Exception e)
            {
                Logger?.LogError($"移动玩家模型时出错: {e.Message}");
            }
        }
        
        // 延迟执行移动玩家模型的协程
        private static System.Collections.IEnumerator DelayedMovePlayerAvatarToFront()
        {
            // 等待一帧，确保UI已经创建完成
            yield return null;
            
            try
            {
                // 通过PlayerAvatarMenuHover组件查找玩家模型对象
                var playerAvatarHover = UnityEngine.Object.FindObjectOfType<PlayerAvatarMenuHover>();
                GameObject? playerAvatarObj = null;
                if (playerAvatarHover != null)
                {
                    playerAvatarObj = playerAvatarHover.transform.parent.gameObject;
                    // 检查父级名称是否正确
                    if (playerAvatarObj.name != "Menu Element Player Avatar")
                    {
                        playerAvatarObj = null; // 如果名称不对，重置为null以便后续查找
                    }
                }
                
                // 如果找不到或名称不对，尝试通过名称查找
                if (playerAvatarObj == null)
                {
                    playerAvatarObj = GameObject.Find("Menu Element Player Avatar");
                }
                
                if (playerAvatarObj != null)
                {
                    // 获取decorationsPage的Transform
                    var menuPage = AccessTools.Field(typeof(REPOPopupPage), "menuPage").GetValue(decorationsPage) as MenuPage;
                    if (menuPage != null)
                    {
                        // 将玩家模型移动到decorationsPage下
                        playerAvatarObj.transform.SetParent(menuPage.transform, true);
                        
                        // 将玩家模型移到最后，使其显示在最前面
                        playerAvatarObj.transform.SetAsLastSibling();
                        
                        // 设置本地坐标
                        playerAvatarObj.transform.localPosition = new Vector3(-76f, -30f, 0f);
                    }
                }
                else
                {
                    Logger?.LogWarning("找不到玩家模型对象");
                }
            }
            catch (Exception e)
            {
                Logger?.LogError($"延迟移动玩家模型时出错: {e.Message}");
            }
        }

        // "关闭所有模型"按钮点击事件
        private static void OnDisableAllButtonClick()
        {
            try
            {
                // 关闭所有装饰物
                HeadDecorationManager.DisableAllDecorations();
                
                // 更新所有标签的按钮文本缓存
                foreach (string tag in ALL_TAGS)
                {
                    if (buttonTextCache.TryGetValue(tag, out var textCache))
                    {
                        foreach (var decoration in HeadDecorationManager.Decorations)
                        {
                            bool shouldShow = tag == "ALL" || 
                                            (decoration.ParentTag?.ToLower() == tag.ToLower());
                            
                            if (shouldShow)
                            {
                                // 更新缓存
                                string buttonText = GetButtonText(decoration, false);
                                textCache[decoration.Name ?? string.Empty] = buttonText;
                            }
                        }
                    }
                }
                
                // 更新当前页面上的按钮状态
                foreach (var decoration in HeadDecorationManager.Decorations)
                {
                    if (decorationButtons.TryGetValue(decoration.Name ?? string.Empty, out REPOButton button))
                    {
                        // 更新按钮文本以反映新状态（全部关闭）
                        button.labelTMP.text = GetButtonText(decoration, false);
                    }
                }
                
                // 更新玩家装饰物
                UpdateDecorations();
                
                // 保存配置
                ConfigManager.SaveConfig();
            }
            catch (Exception e)
            {
                Logger?.LogError($"关闭所有装饰物时出错: {e.Message}");
            }
        }

        // 重新创建UI（供第三方MOD使用）
        public static void RecreateUI()
        {
            try
            {
                // 清空所有缓存的数据
                decorationDataCache.Clear();
                buttonTextCache.Clear();
                decorationButtons.Clear();                
                // 重新初始化数据缓存
                InitializeDataCache();                
                Logger?.LogInfo("UI已重新初始化，缓存已重置");
            }
            catch (Exception e)
            {
                Logger?.LogError($"重新创建UI时出错: {e.Message}");
            }
        }
    }
} 