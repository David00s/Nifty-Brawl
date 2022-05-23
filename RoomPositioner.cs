using System.Collections.Generic;
using UnityEngine;

namespace SpaceApple.MultiRoom
{


    /// <summary>
    /// A simple script to "allocate" position in space for a room
    /// and to store positions that are no longer used. It's quite simple because
    /// all of the game modes are of the same size. If sizes we're different, they would need
    /// to be placed either in further distances, or some fancier spatial algorithm should be used.
    /// </summary>
    public class RoomPositioner: MonoBehaviour
    {
        public int RoomsInRow = 10;
        public int RoomSizeX = 20;
        public int RoomSizeZ = 20;

        private Queue<Vector3> _freedPositions;

        private int _roomIndex;
    
        void Awake()
        {
            _freedPositions = new Queue<Vector3>();
        }

        /// <summary>
        /// Retrieves available position for the room
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public Vector3? GetPositionForRoom(GameRoom room)
        {
            if (_freedPositions.Count > 0)
                return _freedPositions.Dequeue();

            _roomIndex++;
        
            return new Vector3(
                (_roomIndex % RoomsInRow) * RoomSizeX, 
                0,
                (_roomIndex / RoomsInRow) * RoomSizeZ);
        }

        public void FreeUpPosition(Vector3 position)
        {
            _freedPositions.Enqueue(position);
        }
    }

}