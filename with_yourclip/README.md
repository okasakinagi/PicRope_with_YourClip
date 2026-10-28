# 把夹子换成角色头像

这是 [PicRope · 图绳](https://github.com/Renly1994/PicRope) 的个人分支：在**不改动上游逻辑**的前提下，
把绳子上挂图片用的夹子、以及桌面贴图的图钉，换成一排角色头像（每张照片按顺序取一个）。

不想要角色图也完全没问题：读到 `src\assets\basket-clip*.png` 一张都没有时，
程序会自动退回作者原本的矢量夹子和图钉，和上游一模一样。

## 目录约定

| 位置 | 作用 |
| --- | --- |
| `with_yourclip\images\` | **唯一一份**原始角色图（高清、透明背景），提交进仓库 |
| `with_yourclip\make-clip-assets.ps1` | 把原图生成为可内嵌的 384×384 素材 |
| `with_yourclip\README.md` | 本说明 |
| `tools\sync.ps1` | 同步上游更新 |
| `src\ClipArt.cs` 等 4 个源码文件 | 让夹子/图钉支持角色图（见文末清单） |

`src\assets\basket-clip*.png` 是**生成物**，不进仓库（避免同一张图存两份）。
所以刚 clone 下来时，先跑一次生成脚本，否则表现为原版矢量夹子。

## 效果

- 照片按顺序轮换角色；同一张照片在绳子上和在桌面贴图上用的是**同一个角色**
- 角色"手扒在照片上沿"，向上溢出卡片画布，所以**不需要修改挂点和点击热区**
- 文字信封上的夹子保持作者原本的矢量样式（`UsePortrait = false`）

## 换成你自己的图（三步）

1. **准备图片**放进 `with_yourclip\images\`：
   PNG，**必须有透明通道**（背景抠好的那种），短边 ≥ 160px，角色完整、四周留一点空白。
   JPG 不行（没有透明通道）。命名随意，**按文件名排序编号**，想控制顺序就加 `1-`、`2-` 前缀。
2. **生成素材**：

   ```powershell
   .\with_yourclip\make-clip-assets.ps1
   ```

   自动找出每张图的主体包围盒，按统一比例居中放到 **384×384** 透明画布，
   输出 `src\assets\basket-clip1.png`、`basket-clip2.png`……（有几张轮换几张，最多 9 张）。

   只想本地试、不留生成物：加 `-Local`（输出到 `personal\local-assets`）。
3. **构建**：

   ```powershell
   .\build.ps1
   .\build\PicRope.exe
   ```

### 尺寸与位置

| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| `-Canvas` | 384 | 画布边长（必须与上游 `prepare-assets.ps1` 的缩放尺寸一致） |
| `-CharacterHeight` | 176 | 角色高度（像素） |
| `-CharacterTop` | 68 | 角色顶边距画布顶部的距离（像素） |

界面上实际高度 = `44 × (CharacterHeight / Canvas)` ≈ 20px，底边压在照片上沿下方 8px。

## 为什么素材必须是 384×384

上游 1.2.0 起，构建会把 `src\assets\basket-*.png` 交给 `packaging\prepare-assets.ps1`，
**统一拉伸到 384×384（忽略长宽比）**，非正方形素材会被压扁。

让素材本身成为 384×384，这一步就变成无操作。这也是 `src\ClipArt.cs` 用**相对比例**
（`68/384`、`244/384`）而不是绝对像素定位的原因：上游以后即使改缩放尺寸，角色也不会跑偏。

如果上游真的改了那个 384，同时调整 `-Canvas` 和 `src\ClipArt.cs` 里的三个比例常量即可。

## 同步上游更新

```powershell
.\tools\sync.ps1
```

自动完成：拉取上游 → 快进 `main` → 把你的改动重放到最新上游 → 重新生成素材 → 构建验证。

- 你的改动文件**没被上游碰过**：全程无人值守，最后提示推送命令
- **出现冲突**：自动回滚到更新前，列出冲突文件，需要人工处理

可选参数：`-SkipBuild`（跳过构建）、`-SkipFetch`（离线，用本地已有引用）、`-Branch <名字>`。

> 手动等价操作：
> ```powershell
> git fetch upstream
> git checkout main; git merge --ff-only upstream/main
> git checkout custom; git rebase main
> ```

## ⚠️ 关于应用内自动更新

程序内置的更新器盯的是**作者官方仓库**。点「下载并安装」会用官方安装包整包覆盖，
角色图随之消失（源码仍在，重新 `.\build.ps1` 即可恢复）。自己编译使用时不要点它。

另外程序是单实例的：运行新编译的 exe 前，先退出旧实例，或执行
`.\build\PicRope.exe --quit`（`sync.ps1` 会自动处理）。

## 素材与许可

- 代码沿用上游的 MIT 许可（见根目录 `LICENSE` / `NOTICE`）。
- `with_yourclip\images\` 里的角色图为本分支自带，属于原创作品。
- 换成自己的图时，请确认你有权使用并分发。角色图请在授权范围内使用。

## 本分支的改动清单

尽量只碰这几个文件，方便跟着上游走：

| 文件 | 改动 |
| --- | --- |
| `src\ClipArt.cs` | 新增：按 `PicRope.Basket.clip1..N` 加载角色图，缺图返回 `null` |
| `src\ClothesClip.cs` | 夹子支持角色模式（`UsePortrait`），保留原矢量分支 |
| `src\Thumbtack.cs` | 图钉支持角色模式，与绳子上的角色同序 |
| `src\ShelfWindow.cs` | **仅 1 行**：信封的夹子关闭角色模式 |
| `with_yourclip\`、`tools\` | 素材、生成脚本、同步脚本、本说明 |
