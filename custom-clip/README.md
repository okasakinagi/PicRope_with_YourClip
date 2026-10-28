# 把夹子换成角色头像

这是 [PicRope · 图绳](https://github.com/Renly1994/PicRope) 的个人分支，在**不改动上游逻辑**的前提下，
把绳子上挂图片用的夹子、以及桌面贴图的图钉，换成一排角色头像（每张照片按顺序取一个）。

不想要角色图也完全没问题：程序读完 `src\assets\basket-clip*.png` 一张都没有时，
会自动退回作者原本的矢量夹子和图钉，和上游一模一样。

## 效果

- 照片按顺序轮换角色；同一张照片在绳子上和在桌面贴图上用的是**同一个角色**
- 角色"手扒在照片上沿"，向上溢出卡片画布，所以**不需要修改挂点和点击热区**
- 文字信封上的夹子保持作者原本的矢量样式（`UsePortrait = false`）

## 换成你自己的图（三步）

1. **准备图片**：PNG，**必须带透明通道**（背景抠好的那种），短边 ≥ 160px，角色完整、四周留一点空白。JPG 不行（没有透明通道）。
2. **生成素材**：

   ```powershell
   .\custom-clip\make-clip-assets.ps1 -SourceDir "D:\我的角色图"
   ```

   脚本会自动找出每张图的主体包围盒，按统一比例居中放到 **384×384** 的透明画布上，
   依次输出 `src\assets\basket-clip1.png`、`basket-clip2.png`……（按文件名排序）。

   想先本地试试、不污染仓库：加 `-Local`，输出到 `personal\local-assets`。
3. **构建**：

   ```powershell
   .\build.ps1
   .\build\PicRope.exe
   ```

### 图片数量

有几张就轮换几张，最多不要超过 9 张（`basket-clip1..9`）。

### 提交到你的仓库

`src\assets\basket-clip*.png` 默认在 `.git\info\exclude` 里（防止误提交别人的角色图），
确认要上传时强制添加：

```powershell
git add -f src\assets\basket-clip*.png
git commit -m "默认角色图"
```

### 尺寸与位置

角色的显示大小和落位由三个参数决定，都在 `make-clip-assets.ps1` 里：

| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| `-Canvas` | 384 | 画布边长（必须与上游 `prepare-assets.ps1` 的缩放尺寸一致） |
| `-CharacterHeight` | 176 | 角色高度（像素） |
| `-CharacterTop` | 68 | 角色顶边距画布顶部的距离（像素） |

界面上的实际高度 = `44 × (CharacterHeight / Canvas)` ≈ 20px，底边压在照片上沿下方 8px。

## 为什么素材必须是 384×384

上游 1.2.0 起，构建会把 `src\assets\basket-*.png` 交给 `packaging\prepare-assets.ps1`，
**统一拉伸到 384×384（忽略长宽比）**。所以非正方形的素材会被压扁。

让素材本身成为 384×384，这一步就成了无操作，角色形状完全不变。
这也是 `src\ClipArt.cs` 用**相对比例**（`68/384`、`244/384`）而不是绝对像素来定位的原因：
上游以后即使改缩放尺寸，角色大小和位置也不会跑偏。

如果上游真的改了 `prepare-assets.ps1` 里的 384，改 `make-clip-assets.ps1 -Canvas` 和
`src\ClipArt.cs` 里的三个比例常量即可。

## 同步上游更新

```powershell
.\custom-clip\sync.ps1
```

它会自动完成：拉取上游 → 快进 `main` → 把你的改动重放到最新上游 → 构建验证。

- 你的改动文件**没有被上游碰过**：一路自动完成，最后提示推送命令
- **出现冲突**：自动回滚到更新前的状态，并列出冲突文件，需要人工处理（这时候再动手）

`-SkipBuild` 可跳过构建；`-Branch <名字>` 可指定别的分支。

> 手动等价操作：
> ```powershell
> git fetch upstream
> git checkout main; git merge --ff-only upstream/main
> git checkout with_yourclip; git rebase main
> ```

## ⚠️ 关于应用内自动更新

程序内置的更新器盯的是**作者官方仓库**。点「下载并安装」会用官方安装包整包覆盖，
你的角色图会消失（源码仍在，重新 `.\build.ps1` 即可恢复）。

自己编译使用时，建议在托盘 →「更多操作」里不要点更新，或参考上游代码把检查更新关掉。
另外程序有单实例互斥：运行新编译的 exe 之前，先把旧实例退出
（或执行 `.\build\PicRope.exe --quit`）。

## 素材与许可

- 代码部分沿用上游的 MIT 许可（见仓库根目录 `LICENSE` / `NOTICE`）。
- `src\assets\basket-clip*.png` 是本分支自带的默认角色图，为原创作品。
- 替换成自己的图时，请确认你有权使用并分发那些图片。角色图请在授权范围内使用。

## 本分支的改动清单

尽量只有这几个文件，方便跟着上游走：

| 文件 | 改动 |
| --- | --- |
| `src\ClipArt.cs` | 新增：按 `PicRope.Basket.clip1..N` 加载角色图，缺图返回 `null` |
| `src\ClothesClip.cs` | 夹子支持角色模式（`UsePortrait`），保留原矢量分支 |
| `src\Thumbtack.cs` | 图钉支持角色模式，与绳子上的角色同序 |
| `src\ShelfWindow.cs` | **仅 1 行**：信封的夹子关闭角色模式 |
| `custom-clip\*` | 本目录：生成脚本、同步脚本、说明 |
