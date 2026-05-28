# UIKit

轻量级 Unity UI 框架，基于 **UGUI + UniTask**。单栈导航 + 独立弹窗管理。

## 架构

```
UIManager (MonoSingleton)
  ├── ViewStack           ← FullPanel 导航栈（Pause/Resume）
  ├── PopupManager        ← PopupPanel 弹窗管理（Show/Hide/Close）
  ├── PanelProvider       ← FullPanel 工厂（ResourcesProvider）
  └── PopupProvider       ← PopupPanel 工厂（PopupResourcesProvider）
```

## 面板类型

| 类型 | 基类 | 导航 | Pause/Resume | 管理 |
|------|------|------|-------------|------|
| 全屏面板 | `FullPanel` | ViewStack 压栈 | 支持 | `UIManager.Show/Hide/Close` |
| 弹窗 | `PopupPanel` | 不压栈 | 无 | `UIManager.PopupManager.Show/Hide/Close` |

---

## 生命周期

### 状态机

```
Inactive → Active ↔ Paused (仅 FullPanel)
               ↘ Exiting → Destroyed (Close)
```

### FullPanel 调用链

```
═══ Show ═══════════════════════════════════
  UIManager.Show(key) 或 ShowAsync(key)
    └── ViewStack.Push(panel)
          ├── 当前栈顶.Pause()
          │     └── State=Paused → OnPause() → OnPaused 事件
          └── panel.Show()
                ├── 首次: OnCreate() → OnShow() → OnShowed 事件
                └── 非首次: OnShow() → OnShowed 事件

═══ Hide ═══════════════════════════════════
  UIManager.Hide() 或 HideAsync()
    └── ViewStack.Pop()
          ├── 栈顶.Hide()
          │     └── State=Inactive → OnHide() → OnHided 事件 → Provider.Release(面板缓存)
          ├── 下层面板.Resume()
          │     └── State=Active → OnResume() → OnResumed 事件
          └── 返回被隐藏的面板

═══ Close ═══════════════════════════════════
  UIManager.Close(panel) 或 CloseAsync(panel)
    └── panel.Close()
          └── State=Exiting → OnClose() → OnClosed 事件 → Destroy(gameObject)
```

### PopupPanel 调用链

```
═══ Show ═══════════════════════════════════
  PopupManager.ShowAsync<T>(key)
    └── Provider.Load → parent = Provider.Root → panel.Show()
          └── OnCreate() → OnShow() → OnShowed 事件

═══ Hide ═══════════════════════════════════
  PopupManager.HideAsync(key)
    └── panel.Hide()
          └── OnHide() → OnHided 事件 → Provider.Release(缓存)

═══ Close ═══════════════════════════════════
  PopupManager.CloseAsync(key)
    └── panel.Close()
          └── OnClose() → OnClosed 事件 → Destroy
```

### 生命周期钩子一览

| 方法 | 定义类 | 调用时机 | 调用者 |
|------|--------|---------|--------|
| `OnCreate()` | BasePanel | 首次 Show，仅一次 | Show() |
| `OnShow()` | BasePanel | 每次 Show | Show() |
| `OnHide()` | BasePanel | 每次 Hide | Hide() |
| `OnClose()` | BasePanel | Close 时 | Close() |
| `OnPause()` | FullPanel | 被新面板覆盖 | ViewStack.Push |
| `OnResume()` | FullPanel | 上层 Pop 后恢复 | ViewStack.Pop |
| `OnInput(key,down)` | BasePanel | 输入路由 | UIManager |

---

## 公开 API

### UIManager

| 方法 | 返回 | 说明 |
|------|------|------|
| `Show(key)` | void | 显示 FullPanel（Fire & Forget） |
| `ShowAsync(key)` | UniTask\<FullPanel\> | 显示 FullPanel（异步） |
| `Show(key, panel)` | void | 显示已有实例 |
| `ShowAsync(key, panel)` | UniTask | 显示已有实例（异步） |
| `Hide()` | void | 隐藏栈顶（Fire & Forget） |
| `HideAsync()` | UniTask | 隐藏栈顶（异步） |
| `Close(panel)` | void | 关闭面板（Fire & Forget） |
| `CloseAsync(panel)` | UniTask | 关闭面板（异步） |
| `GetActivePanel()` | FullPanel | 获取栈顶面板 |
| `OnInput(key, down)` | bool | 输入路由 |

### UIManager.PopupManager (IPopupManager)

| 方法 | 返回 | 说明 |
|------|------|------|
| `ShowAsync<T>(key)` | UniTask\<T\> | 显示弹窗 |
| `HideAsync(key)` | UniTask | 隐藏弹窗（缓存） |
| `CloseAsync(key)` | UniTask | 关闭弹窗（销毁） |

### SceneUIContext

| 成员 | 说明 |
|------|------|
| `_preplacedPanels` | 场景预置 FullPanel 列表 |
| `Start()` | 自动注册到 UIManager |
| `OnDestroy()` | 自动注销 |

---

## 快速开始

### 1. 创建 FullPanel

```csharp
public class ShopPanel : FullPanel
{
    protected override UniTask OnCreate()
    {
        // 仅一次：获取组件引用
        return UniTask.CompletedTask;
    }

    protected override UniTask OnShow()
    {
        // 每次显示：刷新数据
        return UniTask.CompletedTask;
    }

    protected override UniTask OnPause() => UniTask.CompletedTask;  // 被覆盖
    protected override UniTask OnHide() => UniTask.CompletedTask;   // 隐藏缓存
    protected override UniTask OnClose() => UniTask.CompletedTask;  // 销毁清理

    public override bool OnInput(KeyCode key, bool down)
    {
        if (key == KeyCode.Escape && down) { UIManager.Instance.Hide(); return true; }
        return false;
    }
}
```

### 2. 创建 PopupPanel

```csharp
public class ToastPanel : PopupPanel
{
    protected override UniTask OnShow()
    {
        await UniTask.Delay(2000);
        PopupManager.HideAsync(PanelKey).Forget();
    }
}
```

### 3. 场景配置

1. 创建 GameObject，挂载 `SceneUIContext`
2. 在预置面板列表中拖入场景中的 FullPanel
3. 运行 — Panel 自动注册

### 4. 使用

```csharp
// FullPanel 导航
UIManager.Instance.Show("ShopPanel");           // 打开
UIManager.Instance.Hide();                      // 返回

// Popup 弹窗
await UIManager.Instance.PopupManager.ShowAsync<ToastPanel>("Toast");
```

---

## Provider

| 接口 | 默认实现 | 说明 |
|------|---------|------|
| `IPanelProvider` | `ResourcesProvider` | FullPanel 工厂（Resources.Load） |
| `IPopupProvider : IPanelProvider` | `PopupResourcesProvider` | PopupPanel 工厂（含 Root Canvas） |

热切换时自动迁移缓存：`newProvider.Import(oldProvider.Export())`

---

## 依赖

| 包 | 用途 |
|----|------|
| UniTask | 异步生命周期 |
| Unity uGUI | UI 渲染 |
