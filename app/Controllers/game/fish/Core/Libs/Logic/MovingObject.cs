namespace BanCa.Libs
{
    public class MovingObject
    {
        public int ID;
        public Vector Pos = new Vector();
        public Vector V = new Vector();

        public void Move(float delta)
        {
            Pos.Translate(V.X * delta, V.Y * delta);
        }
    }
}
