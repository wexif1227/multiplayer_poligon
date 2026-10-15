using System;

namespace Poligon.Network
{
    [Serializable]
    public class MpMessage
    {
        public string type, id, room, name, target, killer, weapon, mode, map, message;
        public float x, y, z, yaw, pitch;
        public int hp, damage;
        public bool godMode;
        public MpPlayer[] players;
    }

    [Serializable]
    public class MpPlayer
    {
        public string id, name, weapon;
        public float x, y, z, yaw, pitch;
        public int hp;
        public bool godMode;
    }

    [Serializable]
    public class MpOutgoing
    {
        public string type, name, room, weapon, target, mode, map;
        public float x, y, z, yaw, pitch;
        public int damage;
        public bool godMode;
    }
}
