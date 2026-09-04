namespace GrowGame
{
    /// <summary>
    /// 地块类型。
    /// 对应关卡文本中的字符：
    /// R -> Walkable（可达地块）
    /// X -> Obstacle（障碍物）
    /// S -> Switch（开关）
    /// G -> Door（门）
    /// D -> ItemLocation（道具使用地）
    /// F -> Goal（终点）
    /// V -> VineSource（藤蔓源，会沿预设方向生长）
    /// P / E 不是地块，解析后变成 Walkable 并记录出生点。
    /// </summary>
    public enum TileType
    {
        Walkable,     // 可达地块，玩家和敌人都能通过
        Obstacle,     // 障碍物，双方都不能通过
        Switch,       // 开关，玩家踩下后打开门
        Door,         // 门，初始关闭不可通行，开关触发后打开
        ItemLocation, // 道具使用地，只有这里能使用道具
        Goal,         // 终点，玩家到达后胜利
        VineSource    // 藤蔓源，双方都不能通过，会沿预设方向生长
    }
}
