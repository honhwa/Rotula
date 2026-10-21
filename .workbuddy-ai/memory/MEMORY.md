# Rotula — 项目约定（长期）

## 架构分层
- `src/Rotula.App/Editing/**`：编辑规则（HitTesting、DragSnapper、EditorCommands、ElementBoundsCalculator、DropPlacement），**刻意不依赖 WPF**，以便纯单测。
- `src/Rotula.App/Controls/**`：WPF 控件（DesignCanvas、RulerControl、ToolboxDrag 等）。
- 画布交互全部在 `Controls/DesignCanvas.cs`（自绘：预览位图 + 网格 + 选中框 + 手柄 + 参考线 + 落位框），指针移动不重绘标签。
- 所有对文档的修改都经过 `EditorSession.Edit`（一次撤销步）或 BeginGesture/Touch/EndGesture（拖拽）。

## 本地化
- 键写在 `Resources/Strings.en.resx` 与 `Strings.es.resx`，**必须两边同时加**（`AppLocalizationTests` 校验键集合一致、`{0}` 占位符一致、XAML/代码中用到的键都存在）。
- 代码取文本：`Loc.Get("Key", args)`；XAML：`{loc:T Key}`。

## 测试
- 测试工程 `tests/Rotula.Tests`，共享 `[Collection("App")]`（因为 `Loc.LanguageChanged` 是进程级状态），构造视图模型用 `new MainViewModel(new FakeServices().Build())`。
- 默认标签：100 x 150 mm @203dpi（799 x 1199 dots）；网格默认 1mm，203dpi 下步长 7.99 dots（非整数，吸附断言要按"整数步数"判定）。
