# Unity 编辑器设置说明

本文档说明如何根据现有代码，在 Unity 编辑器中搭建场景、创建预制体、配置关卡并运行游戏。

---

## 一、代码结构总览

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── TileType.cs          地块类型枚举
│   │   ├── LevelData.cs         关卡数据类
│   │   ├── LevelParser.cs       关卡文本解析器
│   │   ├── Pathfinding.cs       怪物最短路径（BFS）
│   │   └── GameManager.cs       游戏核心：回合流程、道具、胜负
│   ├── Map/
│   │   ├── SpriteLibrary.cs     贴图加载（Resources 加载 + 纯色回退）
│   │   ├── GridCell.cs          单个格子
│   │   └── MapGenerator.cs      程序化生成地图
│   ├── Entities/
│   │   ├── PlayerController.cs  玩家输入与移动
│   │   └── MonsterController.cs 怪物位置与被困状态
│   ├── Items/
│   │   └── PlacedItem.cs        道具实例（灌木丛/陷阱）
│   └── UI/
│       └── GameUI.cs            简易 HUD
├── Resources/
│   ├── Levels/level1.txt        第一关关卡文本
│   └── Sprites/说明.txt         素材命名说明
└── Scenes/
    └── ...                      场景文件
```

---

## 二、搭建场景（一个关卡一个场景）

1. 打开 Unity，新建一个场景（`File → New Scene`），保存到 `Assets/Scenes/`，例如 `level1.unity`。

2. **创建 GameManager 物体**：
   - 在 Hierarchy 里右键 `Create Empty`，命名为 `GameManager`。
   - 选中它，在 Inspector 里 `Add Component` → 搜索 `Game Manager`（GrowGame 命名空间下的）并添加。
   - 再 `Add Component` → 搜索 `Map Generator` 并添加。

3. **创建 UI 物体（可选）**：
   - 再 `Create Empty`，命名为 `UI`，添加 `Game UI` 组件，用于显示回合数/道具数/胜负提示。
   - 不加也能玩，只是看不到文字提示。

4. **配置 Map Generator**：
   - `Level File`：点击字段右边圆圈，选择 `Assets/Resources/Levels/level1.txt`。
     （或者清空 `Level File`，在 `Level Resource Name` 里填 `Levels/level1`，两种方式二选一。）
   - `Player Prefab` / `Monster Prefab`：暂时留空也能运行，代码会自动创建；见下文「创建预制体」。
   - `Camera`：把场景里的主摄像机拖进来；勾选 `Auto Fit Camera` 会自动居中并对齐视野。

5. **配置 Game Manager**：
   - `Tile Size` 默认 1，一般不用改。
   - 道具配置：`Bush Count = 2`、`Trap Count = 1`、`Bush Grow Turns = 3`、`Trap Grow Turns = 2`、`Trap Hold Turns = 2`，可按需调整。
   - `Next Scene Name`：通关后要切换的场景名；留空则自动切到 Build Settings 里的下一个场景。

6. **摄像机**：确保场景里有一台 `Camera`，`Projection` 设为 `Orthographic`。
   如果勾选了 `Auto Fit Camera`，运行时会自动定位，无需手动摆位。

7. 点击 Play 运行，用 WASD / 方向键移动玩家。

---

## 三、创建玩家和怪物预制体

> 如果希望玩家/怪物是预制体（便于复用、加动画、挂碰撞体等），按下面步骤做；否则可留空，代码会自动生成简单对象。

### 玩家预制体

1. `Create Empty`，命名 `Player`。
2. 添加组件 `Player Controller`。
3. 添加组件 `Sprite Renderer`，`Sprite` 可以先不设（代码运行时自动从 Resources 加载或显示纯色方块）。
4. 把它从 Hierarchy 拖到 `Assets` 下任意文件夹，生成预制体（原场景里的对象可以删掉）。
5. 把该预制体拖到 `Map Generator` 的 `Player Prefab` 字段。

### 怪物预制体

1. `Create Empty`，命名 `Monster`。
2. 添加组件 `Monster Controller`。
3. 添加组件 `Sprite Renderer`。
4. 拖到 `Assets` 下生成预制体，再把预制体拖到 `Map Generator` 的 `Monster Prefab` 字段。

> 注意：预制体里不要手动改 `Coord` 字段，位置由地图生成时根据关卡文本设定。

---

## 四、关卡文本格式

关卡就是一个 `.txt` 文件，每行是地图的一行，字符含义：

| 字符 | 含义 |
|------|------|
| `.`  | 可达地块（玩家和怪物都能走） |
| `#`  | 障碍物（都不能走） |
| `S`  | 开关（玩家踩下后打开所有门） |
| `G`  | 门（初始关闭不可通行，开关触发后打开） |
| `D`  | 道具使用地（只有站在这里才能用道具） |
| `F`  | 终点（玩家到达即胜利） |
| `V`  | 藤蔓源（会沿预设方向生长，向左） |
| `>`  | 藤蔓源（向右生长） |
| `^`  | 藤蔓源（向上生长） |
| `v`  | 藤蔓源（向下生长） |
| `P`  | 玩家出生点 |
| `E`  | 怪物出生点（可多个） |
| `//` | 注释行（整行忽略） |

示例（`Assets/Resources/Levels/level1.txt`）：

```
#################
#####...###S#####
#####.#D###.#####
E..P..D.......G.F
```

编辑关卡：直接改 `.txt`，或复制一份新文件，再到 `Map Generator` 里重新指定。

---

## 五、放置美术素材

1. 在 `Assets/Resources/Sprites/` 文件夹里放图片（正方形）。
2. 文件名要与 `说明.txt` 里的 key 一致，例如 `player.png`、`tile_floor.png`、`monster.png`。
3. 选中每张图片，在 Inspector 里把 `Texture Type` 设为 `Sprite (2D and UI)`，`Apply`。
4. 没放图片的格子会用纯色方块代替，不影响运行。

我会用到这些 key（详见 `Assets/Resources/Sprites/说明.txt`）：
`tile_floor`、`tile_obstacle`、`tile_switch`、`tile_door`、`tile_door_open`、
`tile_item`、`tile_item_wait`、`tile_goal`、`tile_vine_source`、`tile_vine`、
`player`、`monster`、`item_bush`、`item_trap`、`monster_preview`。

---

## 六、多关卡与场景切换

1. 每个关卡一个场景 + 一个关卡 `.txt`。
2. 在 `Game Manager` 的 `Next Scene Name` 里填下一关的场景名；或留空自动切下一个。
3. 打开 `File → Build Settings`，把 `Assets/Scenes/` 下的所有关卡场景按顺序拖进 `Scenes In Build`（顺序就是通关顺序）。这样留空 `Next Scene Name` 时能自动按顺序切换。

---

## 七、核心规则说明（与代码实现对应）

- **回合**：行动顺序为「玩家行动 → 植物长一回合 → 怪物依次移动（每个怪物一步）→ 回合数 +1」，各阶段之间有 `Stage Delay` 秒停顿（期间玩家输入无效，方便后续加动画）。
- **玩家**：不能原地等待，每回合必须行动——移动一步，或站在 `D` 格放置一个道具；两者都会消耗本回合。
- **被困**：玩家四周没有可走的格子时，判定失败。
- **被抓**：怪物走到玩家所在格，判定失败。
- **终点**：玩家走到 `F` 格，立即胜利，无需等怪物行动。
- **道具一（灌木丛）**：站在 `D` 格按 `1` 放置，生长 `Bush Grow Turns` 回合后该格变成障碍堵路；生长期间（未成熟）若被怪物踩过，会被踩掉、该格变回普通空地（`.`，不再是道具使用地）。
- **道具二（陷阱）**：站在 `D` 格按 `2` 放置，生长 `Trap Grow Turns` 回合后成熟；成熟后怪物踩入即被困 `Trap Hold Turns` 回合并消耗陷阱，该格变回普通空地（`.`，不再是道具使用地）。生长期间（未成熟）若被怪物踩过，同样被踩掉、该格变回普通空地。
- **开关/门**：玩家踩到 `S` 格，所有 `G` 门打开（变为可通行）。
- **怪物 AI**：玩家行动并结算完植物生长后，怪物再根据玩家新位置，用 BFS 求最短路径走一步；多条最短路径时按「上、下、左、右」固定顺序选择。不再预先显示怪物轨迹，避免玩家据此无限遛怪。

---

## 八、藤蔓机制

藤蔓从 `V` 源出发，沿预设方向（`V`=左、`>`=右、`^`=上、`v`=下）每个回合生长一格，长满指定格数后停止。

- 生长规则：下一格**必须是道路（`.`）**，且不能被玩家、怪物占用，否则该回合不生长（下回合继续尝试）。
- 长出来的藤蔓会变成不可通行的障碍，玩家和怪物都不能穿过。
- 生长长度：默认 4 格，可在 `Game Manager` 的 `Default Vine Length` 里改（对所有未单独指定长度的藤蔓生效）。

示例（`Assets/Resources/Levels/level2.txt`）：

```
#########
#P...#.F#
##D..#..#
#.......#
#..##.#.#
#E...V#.#
#########
```

其中 `V` 在右侧，向左生长 4 格；`M`（转换后为 `E`）是怪物，`G`（转换后为 `F`）是终点，`T`（转换后为 `D`）是机关格。

> 藤蔓文档里用 `M` 表示怪物、`G` 表示终点、`T` 表示机关格，这些与本文档字符集不同，转换时分别映射为 `E`、`F`、`D`。
