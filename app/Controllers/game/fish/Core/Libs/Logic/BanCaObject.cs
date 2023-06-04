using differ.shapes;
using SimpleJSON;
using System;
using System.Collections.Generic;
using Utils;

namespace BanCa.Libs
{
    public class BanCaObject : MovingObject
    {
        static readonly float pi_over_2 = (float)(MathHaxe.PI / 2);
        const bool compress = true;

        public bool SpeedUp = true; // use in jump, first accelerate to max speed then decelerate to 0
        public Vector A = new Vector(Config.JUMP_ACCELERATION_OCTOPUS, 0);
        public float MaxHealth, Health; // offset by bullet power, die when <= 0
        public long Value; // value add by bullet value on hit, last player claim value on fish die
        public long MaxValue = 1; // fish will gradually bigger when its value rise
        public Config.MoveType MoveType;
        public Config.MoveType NextMoveType;
        public int MoveCount; // change to next move type when 0
        public float TurnAngleRadian; // How much to turn
        public float TurnTimeS; // how much time each turn
        public float Width, Height;
        public Config.FishType Type;

        public Config.FishType OriginalType;

        public float NextV = 0;
        public int NextMoveCount;
        public float NextTurnAngleRadian;
        public float NextTurnTimeS;

        public Polygon BoundingBox;

        public Config.MoveType[] allowMoveTypes = null; // null = all

        float turnTimeCount;
        GameBanCa world;
        Vector direction = new Vector(0, 1);

        bool isShadow = false;
        bool followShadow = false;
        float followTime = 0;
        BanCaObject shadow;

        internal long lastTimeStamp = 0; // all update msg that smaller than this timestamp will be discarded

        Vector tempV = new Vector();

        public HashSet<string> whoShoots = new HashSet<string>();
        public float EnlargeRate { get; private set; }

        public BanCaObject MimicTarget = null; // copy this target movement

        internal delegate bool SkipNextMove(BanCaObject banCaObject);
        internal Action<BanCaObject> onDie, onHitBound, onRevive;
        internal SkipNextMove onStartNextMove; // return true to skip setup next move
        internal Action<BanCaObject> onEndNextMove;
        internal bool isSpecial = false;
        internal int moveIndex = 0;
        internal long reviveValue = 0; // on revive value set to this instead
        internal float reviveHealth = 0; // on revive health set to this instead

        public Path Path = new Path(); // move by path if it is not empty

        public BanCaObject(GameBanCa world)
        {
            this.world = world;

            MaxHealth = Health = 1000;
            Value = 0;
            MoveCount = 30 + world.Random.Next() % 30;
            TurnAngleRadian = Config.TURN_ANGLE_RAD;
            TurnTimeS = Config.TURN_TIME;
            Pos.Set(Config.WorldX + (float)(Config.WorldW * world.Random.NextDouble()), Config.WorldY + (float)(Config.WorldH * world.Random.NextDouble()));
            NextV = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
            V.Set(NextV, 0);
            V.Rotate((float)(360 * world.Random.NextDouble()) * 3.14f / 180);
            direction.Set(V).Normalize();

            //Width = Config.MIN_WIDTH + (float)(Config.MAX_VARY_WIDTH * world.Random.NextDouble());
            //Height = Config.MIN_HEIGHT + (float)(Config.MAX_VARY_HEIGHT * world.Random.NextDouble());
            Width = 1;
            Height = 1;
            BoundingBox = Polygon.rectangle(Pos.X, Pos.Y, Width, Height, true);

            if (world.IsServer)
                setUpNextMove(false);
        }

        public float GetAccOrDcc()
        {
            if (Type == Config.FishType.Octopus)
            {
                return SpeedUp ? Config.JUMP_ACCELERATION_OCTOPUS : -Config.JUMP_DECCELERATION_OCTOPUS;
            }
            else if (Type == Config.FishType.Cuttle)
            {
                return SpeedUp ? Config.JUMP_ACCELERATION_CUTTLE : -Config.JUMP_DECCELERATION_CUTTLE;
            }
            else if (Type == Config.FishType.SeaTurtle)
            {
                return SpeedUp ? Config.JUMP_ACCELERATION_SEA_TURTLE : -Config.JUMP_DECCELERATION_SEA_TURTLE;
            }
            return 0;
        }

        public void UpdateBound()
        {
            if (Type == Config.FishType.CaThanTai || Type == Config.FishType.GoldenFrog || Type == Config.FishType.Phoenix
                || Type == Config.FishType.Shark || Type == Config.FishType.MermaidBig || Type == Config.FishType.MerMan)
            {
                Polygon.updateRectangle(BoundingBox, Pos.X, Pos.Y, Width, Height, true);
                EnlargeRate = 1f;
            }
            else
            {
                MaxValue = FishFactory.CalculateMaxValue(MaxHealth, world.TableBlindIndex);
                var enlargeRate = 1f + (Config.MaxFillScale - 1) * ((float)Value / MaxValue);
                if (enlargeRate > Config.MaxFillScale)
                {
                    enlargeRate = Config.MaxFillScale;
                }

                EnlargeRate = enlargeRate;
                Polygon.updateRectangle(BoundingBox, Pos.X, Pos.Y, enlargeRate * Width, enlargeRate * Height, true);
            }
        }

        public void GeneratePath(int maxSegment)
        {
            Path.Clear();
            if (Health <= 0)
                return;
            var now = TimeUtil.TimeStamp;
            Path.AddNode(now, Pos.X, Pos.Y);
            var time = MoveCount * TurnTimeS;
            now += (long)(time * 1000);
            if (V.SquareLength < Config.MIN_SPEED_2)
            {
                if (Pos.SquareLength >= Config.MIN_SPEED_2)
                {
                    V.Set(-Pos.X, -Pos.Y).Normalize().Mul(Config.MIN_SPEED);
                }
                else
                {
                    V.Set(-1, 0).Mul(Config.MIN_SPEED);
                }
            }
            Path.AddNode(now, Pos.X + V.X * time, Pos.Y + V.Y * time);
            int segCount = 1;

            while (segCount < maxSegment)
            {
                var moveType = allowMoveTypes == null ? (Config.MoveType)(world.Random.Next() % (int)Config.MoveType.All)
                    : allowMoveTypes[(int)(world.Random.Next() % allowMoveTypes.Length)];
                //var moveType = Config.MoveType.Straight;
                var moveCount = 20 + world.Random.Next() % 30;
                var speed = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
                V.Normalize().Mul(speed);
                if (moveType == Config.MoveType.TurnLeft || moveType == Config.MoveType.JumpLeft)
                {
                    moveCount /= 4;
                    for (int i = 0; i < moveCount; i++)
                    {
                        V.Rotate(TurnAngleRadian);
                        time = TurnTimeS;
                        now += (long)(time * 1000);
                        var lastNode = Path.LastNode;
                        Path.AddNode(now, lastNode.X + V.X * time, lastNode.Y + V.Y * time);

                        // out of bound
                        lastNode = Path.LastNode;
                        if (lastNode.X < Config.WorldX || lastNode.X > Config.WorldX + Config.WorldW ||
                            lastNode.Y < Config.WorldY || lastNode.Y > Config.WorldY + Config.WorldH)
                        {
                            break;
                        }
                    }
                }
                else if (moveType == Config.MoveType.TurnRight || moveType == Config.MoveType.JumpRight)
                {
                    moveCount /= 4;
                    for (int i = 0; i < moveCount; i++)
                    {
                        V.Rotate(-TurnAngleRadian);
                        time = TurnTimeS;
                        now += (long)(time * 1000);
                        var lastNode = Path.LastNode;
                        Path.AddNode(now, lastNode.X + V.X * time, lastNode.Y + V.Y * time);

                        // out of bound
                        lastNode = Path.LastNode;
                        if (lastNode.X < Config.WorldX || lastNode.X > Config.WorldX + Config.WorldW ||
                            lastNode.Y < Config.WorldY || lastNode.Y > Config.WorldY + Config.WorldH)
                        {
                            break;
                        }
                    }
                }
                else if (moveType == Config.MoveType.Straight || moveType == Config.MoveType.Jump) // do nothing
                {
                    time = moveCount * TurnTimeS;
                    now += (long)(time * 1000);
                    var lastNode = Path.LastNode;
                    Path.AddNode(now, lastNode.X + V.X * time, lastNode.Y + V.Y * time);
                }

                segCount++;
                // out of bound
                {
                    var lastNode = Path.LastNode;
                    if (lastNode.X < Config.WorldX || lastNode.X > Config.WorldX + Config.WorldW ||
                        lastNode.Y < Config.WorldY || lastNode.Y > Config.WorldY + Config.WorldH)
                    {
                        break;
                    }
                }
            }
        }

        public void Update(float delta)
        {
            if (Health <= 0)
                return;

            if (!Path.IsEmpty)
            {
                float _x = 0;
                float _y = 0;
                Path.GetPositionByTime(TimeUtil.TimeStamp, ref _x, ref _y, ref direction, out var outOfRange);
                if (outOfRange)
                {
                    if (isSpecial)
                    {
                        var pointIndex = (int)(world.Random.NextDouble() * world.RespawnPoints.Count);
                        Pos.Set(world.RespawnPoints[pointIndex]);

                        MoveType = allowMoveTypes == null ? Config.MoveType.Straight : allowMoveTypes[0];
                        MoveCount = 5;
                        TurnTimeS = Config.TURN_TIME;
                        turnTimeCount = TurnTimeS;
                        direction.Set(-Pos.X + (float)(world.Random.NextDouble() * 0.6 - 0.3) * Config.ScreenW, -Pos.Y + (float)(world.Random.NextDouble() * 0.6 - 0.3) * Config.ScreenH).Normalize();
                        NextV = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
                        V.Set(direction).Mul(NextV);
                        UpdateBound();
                        GeneratePath(200);
                        pushUpdate();
                    }
                    else
                    {
                        if (world.WorldState == State.Playing)
                        {
                            Health = -1;
                            if (world.IsServer)
                            {
                                Remove();
                                world.scheduleToRevive(this, Value, Health);
                                Value = 0;
                                pushRemove();
                            }
                        }
                    }

                    if (onHitBound != null)
                    {
                        onHitBound(this);
                    }
                }
                else
                {
                    Pos.Set(_x, _y);
                    BoundingBox.set_x(Pos.X);
                    BoundingBox.set_y(Pos.Y);
                    V.Set(direction);
                    BoundingBox.set_rotation_rad(V.GetAngle() - pi_over_2);

                    if (_x < Config.WorldX || _x > Config.WorldX + Config.WorldW ||
                        _y < Config.WorldY || _y > Config.WorldY + Config.WorldH)
                    {
                        if (world.WorldState == State.Playing && !isSpecial)
                        {
                            Health = -1;
                            if (world.IsServer)
                            {
                                Remove();
                                world.scheduleToRevive(this, Value, Health);
                                Value = 0;
                                pushRemove();
                            }
                        }

                        if (onHitBound != null)
                        {
                            onHitBound(this);
                        }
                    }
                }
            }
            else
            {
                if (followShadow)
                {
                    followTime -= delta;
                    this.Move(followTime >= 0 ? delta : delta + followTime);

                    BoundingBox.set_x(Pos.X);
                    BoundingBox.set_y(Pos.Y);

                    if (followTime <= 0)
                    {
                        followShadow = false;
                        copyValue(shadow);
                    }
                }
                else
                {
                    if (world.WorldState != State.WaitingForNewWave)
                    {
                        turnTimeCount -= delta;
                        if (turnTimeCount <= 0)
                        {
                            var subDelta = turnTimeCount + delta;
                            MoveCount--;
                            turnTimeCount += TurnTimeS;
                            doMove(subDelta);
                            delta = delta - subDelta;
                            doTurn();
                            if (MoveCount < 0)
                            {
                                moveIndex++;
                                MoveType = NextMoveType;
                                MoveCount = NextMoveCount;
                                TurnAngleRadian = NextTurnAngleRadian;
                                TurnTimeS = NextTurnTimeS;
                                turnTimeCount = TurnTimeS;
                                NextMoveType = Config.MoveType.None;

                                if (V.SquareLength >= Config.MIN_SPEED_2)
                                {
                                    direction.Set(V.Normalize());
                                    V.Mul(NextV);
                                }
                                else
                                {
                                    V.Set(direction).Mul(NextV);
                                }

                                if (world.IsServer) // only server author what to do next
                                {
                                    setUpNextMove();

                                    if (MoveType == Config.MoveType.None) // somehow we out of sync
                                    {
                                        SetMimicTarget(MimicTarget, true); // resync
                                    }
                                }
                            }
                        }
                    }

                    doMove(delta);
                }

                if (world.IsServer) // only server author what to do next
                {
                    if (MimicTarget != null)
                    {
                        if (MimicTarget.Health <= 0)
                        {
                            MimicTarget = null;
                            if (NextMoveType == Config.MoveType.None) // polling mimic target for next type
                            {
                                setUpNextMove(); // have to move on its own, target is dead
                            }
                        }
                        else
                        {
                            if (NextMoveType == Config.MoveType.None && MimicTarget.NextMoveType != Config.MoveType.None) // polling mimic target for next type
                            {
                                if (MimicTarget.moveIndex == moveIndex) // make sure mimic target has new next move type
                                {
                                    NextMoveType = MimicTarget.NextMoveType;
                                    NextMoveCount = MimicTarget.NextMoveCount;
                                    NextTurnAngleRadian = MimicTarget.NextTurnAngleRadian;
                                    NextTurnTimeS = MimicTarget.NextTurnTimeS;
                                    A.Set(MimicTarget.A);
                                    NextTurnAngleRadian = MimicTarget.NextTurnAngleRadian;
                                    NextV = MimicTarget.NextV;

                                    pushUpdate();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void doMove(float delta)
        {
            if (MoveType == Config.MoveType.Jump || MoveType == Config.MoveType.JumpLeft || MoveType == Config.MoveType.JumpRight)
            {
                var ovx = direction.X;
                var ovy = direction.Y;

                V.Translate(delta * A.X, delta * A.Y); // vx = vx + delta * ax
                if (ovx * V.X < 0) // V change direction
                {
                    var extraTime = V.X / A.X; // time over shoot since V become 0
                    V.Set(0, 0);
                    SpeedUp = true;
                    A.Set(direction).Mul(GetAccOrDcc());

                    if (extraTime > 0)
                    {
                        delta = extraTime;
                        V.Translate(delta * A.X, delta * A.Y);
                    }
                }
                else if (ovy * V.Y < 0)
                {
                    var extraTime = V.Y / A.Y; // time over shoot since V become 0
                    V.Set(0, 0);
                    SpeedUp = true;
                    A.Set(direction).Mul(GetAccOrDcc());

                    if (extraTime > 0)
                    {
                        delta = extraTime;
                        V.Translate(delta * A.X, delta * A.Y);
                    }
                }
                else if (V.SquareLength >= Config.MAX_JUMP_SPEED_2)
                {
                    var extraTime = (float)((Math.Sqrt(V.SquareLength) - Config.MAX_JUMP_SPEED) / Math.Abs(GetAccOrDcc())); // time over shoot since V become max
                    this.Move(delta - extraTime); // move while V is max

                    SpeedUp = false;
                    V.Normalize().Mul(Config.MAX_JUMP_SPEED);
                    A.Set(direction).Mul(GetAccOrDcc());

                    if (extraTime > 0)
                    {
                        delta = extraTime;
                        V.Translate(delta * A.X, delta * A.Y);
                    }
                }
            }

            this.Move(delta);

            BoundingBox.set_x(Pos.X);
            BoundingBox.set_y(Pos.Y);
        }

        public void CheckBound(float x, float y, float w, float h)
        {
            if (!Path.IsEmpty) return;

            bool hitBound = false;
            bool moveFromOutSide = false;
            if (Pos.X < x)
            {
                if (V.X < 0) V.X = -V.X;
                else moveFromOutSide = true;
                if (world.WorldState != State.NewWave && !moveFromOutSide) // follower can be summon out of bound, so skip teleport
                    Pos.X = x + x - Pos.X;
                hitBound = true;
            }
            if (Pos.X > x + w)
            {
                if (V.X > 0) V.X = -V.X;
                else moveFromOutSide = true;
                if (world.WorldState != State.NewWave && !moveFromOutSide)
                    Pos.X = x + w - (Pos.X - (x + w));
                hitBound = true;
            }

            if (Pos.Y < y)
            {
                if (V.Y < 0) V.Y = -V.Y;
                else moveFromOutSide = true;
                if (world.WorldState != State.NewWave && !moveFromOutSide)
                    Pos.Y = y + y - Pos.Y;
                hitBound = true;
            }
            if (Pos.Y > y + h)
            {
                if (V.Y > 0) V.Y = -V.Y;
                else moveFromOutSide = true;
                if (world.WorldState != State.NewWave && !moveFromOutSide)
                    Pos.Y = y + h - (Pos.Y - (y + h));
                hitBound = true;
            }

            if (V.SquareLength >= Config.MIN_SPEED_2)
            {
                BoundingBox.set_rotation_rad(V.GetAngle() - pi_over_2);
                direction.Set(V).Normalize();
            }

            if (hitBound)
            {
                if (MoveType == Config.MoveType.Jump || MoveType == Config.MoveType.JumpLeft || MoveType == Config.MoveType.JumpRight)
                {
                    A.Set(direction).Mul(GetAccOrDcc());
                }
                BoundingBox.set_x(Pos.X);
                BoundingBox.set_y(Pos.Y);

                if (!moveFromOutSide)
                {
                    if (world.WorldState == State.Playing && !isShadow && !isSpecial)
                    {
                        Health = -1;
                        if (world.IsServer)
                        {
                            Remove();
                            world.scheduleToRevive(this, Value, Health);
                            Value = 0;
                            pushRemove();
                        }
                    }

                    if (onHitBound != null)
                    {
                        onHitBound(this);
                    }
                }
            }
        }

        // set this behind data
        public void PlaceBehind(BanCaObject data)
        {
            var m = Width > Height ? Width : Height;
            var rad = (float)((world.Random.NextDouble() - 0.5) * 3.1416 / 2);
            this.Pos.Set(-(world.Random.Next(1200, 1600) / 1000f) * m * data.direction.X, -(world.Random.Next(1200, 1600) / 1000f) * m * data.direction.Y).Rotate(rad).Add(data.Pos);
            UpdateBound();
        }

        public void SetMimicTarget(BanCaObject data, bool push = false)
        {
            MimicTarget = data;
            if (data != null && data.Health > 0)
            {
                A.Set(data.A);
                SpeedUp = data.SpeedUp;
                moveIndex = data.moveIndex;
                MoveCount = data.MoveCount;
                MoveType = data.MoveType;
                NextMoveType = data.NextMoveType;
                TurnAngleRadian = data.TurnAngleRadian;
                TurnTimeS = data.TurnTimeS;
                V.Set(data.V);

                NextV = data.NextV;
                NextMoveCount = data.NextMoveCount;
                NextTurnAngleRadian = data.NextTurnAngleRadian;
                NextTurnTimeS = data.NextTurnTimeS;

                turnTimeCount = data.turnTimeCount + 0.05f; // to make sure follower update a bit slower than target
                direction.Set(data.direction);

                UpdateBound();

                if (push)
                {
                    pushUpdate();
                }
            }
            else
            {
                MimicTarget = null;
                if (NextMoveType == Config.MoveType.None) // have to move on its own, target is dead
                {
                    if (MoveType == Config.MoveType.None)
                    {
                        setUpNextMove(false);
                        MoveType = NextMoveType;
                        MoveCount = NextMoveCount;
                        TurnAngleRadian = NextTurnAngleRadian;
                        TurnTimeS = NextTurnTimeS;
                        turnTimeCount = TurnTimeS;

                        setUpNextMove();
                    }
                    else
                    {
                        setUpNextMove();
                    }
                }
                else
                {
                    if (MoveType == Config.MoveType.None)
                    {
                        setUpNextMove(false);
                        MoveType = NextMoveType;
                        MoveCount = NextMoveCount;
                        TurnAngleRadian = NextTurnAngleRadian;
                        TurnTimeS = NextTurnTimeS;
                        turnTimeCount = TurnTimeS;

                        setUpNextMove();
                    }
                }
            }
        }

        public void Remove()
        {
            Pos.Set(2000, 2000);
            V.Set(0, 0);
            BoundingBox.set_x(Pos.X);
            BoundingBox.set_y(Pos.Y);
            followShadow = false;
            MimicTarget = null;
            Path.Clear();
            if (onDie != null)
            {
                onDie(this);
            }
        }

        public void SetRefundOnRevive()
        {
            //if (Type == Config.FishType.CaThanTai)
            //{
            //    reviveValue += world.Profit > 0 ? world.Profit * 4 / 100 : 0; // refund 4%
            //}
            //else if (Type == Config.FishType.Phoenix)
            //{
            //    reviveValue += world.Profit > 0 ? world.Profit * 3 / 100 : 0; // refund 3%
            //}
            //else if (Type == Config.FishType.MerMan || Type == Config.FishType.Shark)
            //{
            //    reviveValue += world.Profit > 0 ? world.Profit * 2 / 100 : 0; // refund 2%
            //}
            //else if (Type == Config.FishType.MermaidBig)
            //{
            //    reviveValue += world.Profit > 0 ? world.Profit * 1 / 100 : 0; // refund 1%
            //}
        }

        public void OnHit(BanCaBullet bullet)
        {
            Health -= bullet.Power;
            Value += bullet.Value;
            whoShoots.Add(bullet.PlayerId);
            UpdateBound();
        }

        private void doTurn()
        {
            if (MoveType == Config.MoveType.TurnLeft)
            {
                V.Rotate(TurnAngleRadian);
            }
            else if (MoveType == Config.MoveType.TurnRight)
            {
                V.Rotate(-TurnAngleRadian);
            }
            else if (MoveType == Config.MoveType.JumpLeft)
            {
                var vv = (float)MathHaxe.sqrt(V.SquareLength);
                direction.Rotate(TurnAngleRadian);
                V.Set(direction).Mul(vv);
                A.Set(direction).Mul(GetAccOrDcc());
            }
            else if (MoveType == Config.MoveType.JumpRight)
            {
                var vv = (float)MathHaxe.sqrt(V.SquareLength);
                direction.Rotate(-TurnAngleRadian);
                V.Set(direction).Mul(vv);
                A.Set(direction).Mul(GetAccOrDcc());
            }
            else if (MoveType == Config.MoveType.Jump) // do nothing
            {

            }
            else if (MoveType == Config.MoveType.Straight) // do nothing
            {

            }
        }

        private void setUpNextMove(bool push = true)
        {
            if (onStartNextMove != null)
            {
                if (onStartNextMove(this))
                {
                    return;
                }
            }

            if (MimicTarget != null && MimicTarget.Health > 0)
            {
                NextMoveType = Config.MoveType.None;
                return;
            }

            NextMoveType = allowMoveTypes == null ? (Config.MoveType)(world.Random.Next() % (int)Config.MoveType.All)
                : allowMoveTypes[(int)(world.Random.Next() % allowMoveTypes.Length)];
            //NextMoveType = MoveType.Straight;
            //NextMoveType = MoveType.TurnRight;
            //NextMoveType = Config.MoveType.JumpRight;
            NextMoveCount = 10 + world.Random.Next() % 10;
            NextTurnAngleRadian = Config.TURN_ANGLE_RAD;
            NextTurnTimeS = Config.TURN_TIME;

            // try focus fish to center of the map
            var sign = -direction.X * Pos.Y + direction.Y * Pos.X;
            if (NextMoveType == Config.MoveType.TurnLeft || NextMoveType == Config.MoveType.TurnRight)
            {
                NextMoveType = sign > 0 ? Config.MoveType.TurnLeft : Config.MoveType.TurnRight;
                //NextMoveCount /= 2; // shorter turn
            }
            else if (NextMoveType == Config.MoveType.JumpLeft || NextMoveType == Config.MoveType.JumpRight)
            {
                NextMoveType = sign > 0 ? Config.MoveType.JumpLeft : Config.MoveType.JumpRight;
                //NextMoveCount /= 2; // shorter turn
            }

            if (NextMoveType == Config.MoveType.TurnLeft)
            {
                NextV = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
            }
            else if (NextMoveType == Config.MoveType.TurnRight)
            {
                NextV = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
            }
            else if (NextMoveType == Config.MoveType.Jump || NextMoveType == Config.MoveType.JumpLeft || NextMoveType == Config.MoveType.JumpRight)
            {
                A.Set(direction).Mul(GetAccOrDcc());
                NextTurnAngleRadian = Config.JUMP_TURN_ANGLE_RAD;
                NextV = 0;
            }
            else if (NextMoveType == Config.MoveType.Straight)
            {
                NextV = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
            }

            if (onEndNextMove != null)
            {
                onEndNextMove(this);
            }

            if (push)
                pushUpdate();
        }

        internal void ToSpawnPoint()
        {
            MimicTarget = null;
            var pointIndex = (int)(world.Random.NextDouble() * world.RespawnPoints.Count);
            Pos.Set(world.RespawnPoints[pointIndex]);
            followShadow = false;
            MoveType = allowMoveTypes == null ? Config.MoveType.Straight : allowMoveTypes[0];
            MoveCount = 10;
            moveIndex++;
            TurnTimeS = Config.TURN_TIME;
            turnTimeCount = TurnTimeS;
            direction.Set(-Pos.X + (float)(world.Random.NextDouble() * 0.6 - 0.3) * Config.ScreenW, -Pos.Y + (float)(world.Random.NextDouble() * 0.6 - 0.3) * Config.ScreenH).Normalize();
            NextV = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
            V.Set(direction).Mul(NextV);
            UpdateBound();
            setUpNextMove(false);

            pushTeleport();
        }

        internal void Revive(bool push = true)
        {
            //UnityEngine.Debug.Log("Revive the fish: " + ID);
            MimicTarget = null;
            if (reviveHealth > 0) Health = reviveHealth;
            else FishFactory.RandomHealth(this, world.Random);
            reviveHealth = -1;
            Value = reviveValue;
            reviveValue = 0;
            var pointIndex = (int)(world.Random.NextDouble() * world.RespawnPoints.Count);

            if (world.WorldState == State.WaitingForNewWave)
            {
                V.Set(0, 0);
            }
            else
            {
                Pos.Set(world.RespawnPoints[pointIndex]);
            }
            followShadow = false;
            MoveType = allowMoveTypes == null ? Config.MoveType.Straight : allowMoveTypes[0];
            MoveCount = 5;
            moveIndex++;
            TurnTimeS = Config.TURN_TIME;
            turnTimeCount = TurnTimeS;
            direction.Set(-Pos.X + (float)(world.Random.NextDouble() * 0.6 - 0.3) * Config.ScreenW, -Pos.Y + (float)(world.Random.NextDouble() * 0.6 - 0.3) * Config.ScreenH).Normalize();
            NextV = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
            V.Set(direction).Mul(NextV);
            UpdateBound();
            whoShoots.Clear();
            setUpNextMove(push);

            if (onRevive != null)
            {
                onRevive(this);
            }
        }

        public void ForceSetDirection()
        {
            if (V.SquareLength >= Config.MIN_SPEED_2)
            {
                direction.Set(V).Normalize();
                BoundingBox.set_rotation_rad(V.GetAngle() - pi_over_2);
            }
        }

        public void SetMoveType()
        {
            NextMoveType = MoveType = allowMoveTypes == null ? Config.MoveType.Straight : allowMoveTypes[0];
            if (NextMoveType == Config.MoveType.TurnLeft)
            {
                NextV = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
            }
            else if (NextMoveType == Config.MoveType.TurnRight)
            {
                NextV = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
            }
            else if (NextMoveType == Config.MoveType.Jump || NextMoveType == Config.MoveType.JumpLeft || NextMoveType == Config.MoveType.JumpRight)
            {
                SpeedUp = true;
                A.Set(direction).Mul(GetAccOrDcc());
                NextV = 0;
            }
            else if (NextMoveType == Config.MoveType.Straight)
            {
                NextV = (float)(Config.MIN_SPEED + Config.MAX_VARY_SPEED * world.Random.NextDouble());
            }
        }

        public void pushUpdate()
        {
            if (world.OnUpdateObject != null && (world.WorldState == State.Playing || world.WorldState == State.NewWave))
            {
                var json = ToJson();
                json["id"] = ID;
                json["time"] = TimeUtil.TimeStamp;
                world.OnUpdateObject(json);
                //Logger.Log("Push update: " + json.ToString());
            }
        }

        public void pushTeleport()
        {
            if (world.OnObjectTeleport != null && (world.WorldState == State.Playing || world.WorldState == State.NewWave))
            {
                var json = ToJson();
                json["id"] = ID;
                json["time"] = TimeUtil.TimeStamp;
                world.OnObjectTeleport(json);
                //Logger.Log("Push update: " + json.ToString());
            }
        }

        public void pushUpdateSequence()
        {
            if (world.OnUpdateObjectSequence != null && (world.WorldState == State.Playing || world.WorldState == State.NewWave))
            {
                var json = ToJson();
                json["id"] = ID;
                json["time"] = TimeUtil.TimeStamp;
                world.OnUpdateObjectSequence(json);
                //Logger.Log("Push update seq: " + json.ToString());
            }
        }

        public void pushRemove()
        {
            if (world.OnObjectDie != null)
            {
                var msg = new JSONObject();
                msg["playerId"] = "";
                msg["id"] = ID;
                msg["value"] = 0;
                msg["time"] = TimeUtil.TimeStamp;
                world.OnObjectDie(msg);
            }
        }

        public JSONNode ToJson(bool hideHp = true, bool overrideCompress = true)
        {
            if (compress && overrideCompress)
            {
                var data = new JSONObject();
                data["id"] = ID;
                data["px"] = Pos.X;
                data["py"] = Pos.Y;
                //data["vx"] = V.X;
                //data["vy"] = V.Y;
                //data["ax"] = A.X;
                //data["ay"] = A.Y;
                //data["s"] = SpeedUp;
                //data["h"] = Health;
                data["h"] = (Config.HideFishHp != 0 && hideHp) ? (Health > 0 ? 1 : -1) : Health;
                //data["V"] = Value;
                //data["m"] = (int)MoveType;
                //data["n"] = (int)NextMoveType;
                //data["M"] = MoveCount;
                //data["tu"] = TurnAngleRadian;
                //data["ts"] = TurnTimeS;
                //data["nv"] = NextV;
                //data["nc"] = NextMoveCount;
                //data["nr"] = NextTurnAngleRadian;
                //data["ns"] = NextTurnTimeS;
                data["w"] = Width;
                data["H"] = Height;
                data["t"] = (int)Type;
                data["dx"] = direction.X;
                data["dy"] = direction.Y;
                //data["tt"] = turnTimeCount;
                data["path"] = Path.ToJson();
                return data;
            }
            else
            {
                var data = new JSONObject();
                data["id"] = ID;
                data["pos"] = Pos.ToJson();
                //data["v"] = V.ToJson();
                //data["a"] = A.ToJson();
                //data["speedUp"] = SpeedUp;
                data["health"] = Health;
                //data["value"] = Value;
                //data["moveType"] = (int)MoveType;
                //data["nextMoveType"] = (int)NextMoveType;
                //data["moveCount"] = MoveCount;
                //data["turnAngleRadian"] = TurnAngleRadian;
                //data["turnTimeS"] = TurnTimeS;
                //data["nextV"] = NextV;
                //data["nextMoveCount"] = NextMoveCount;
                //data["nextTurnAngleRadian"] = NextTurnAngleRadian;
                //data["nextTurnTimeS"] = NextTurnTimeS;
                data["width"] = Width;
                data["height"] = Height;
                data["type"] = (int)Type;
                data["direction"] = direction.ToJson();
                //data["turnTimeCount"] = turnTimeCount;
                data["path"] = Path.ToJson();
                return data;
            }
        }

        public void ParseJson(JSONNode data)
        {
            //UnityEngine.Debug.Log("Set health the fish: " + ID + " is shadow " + isShadow);
            if (compress)
            {
                ID = data["id"].AsInt;
                Pos.X = data["px"].AsFloat;
                Pos.Y = data["py"].AsFloat;
                V.X = data["vx"].AsFloat;
                V.Y = data["vy"].AsFloat;
                A.X = data["ax"].AsFloat;
                A.Y = data["ay"].AsFloat;
                SpeedUp = data["s"].AsBool;
                Health = data["h"].AsFloat;
                if (world.IsServer)
                {
                    Value = data["V"].AsLong;
                }
                //else
                //{
                //    var _Value = data["V"].AsLong;
                //    if(_Value > Value) // fake on client, prevent fish suddenly become smaller
                //    {
                //        Value = _Value;
                //    }
                //}
                MoveType = (Config.MoveType)data["m"].AsInt;
                NextMoveType = (Config.MoveType)data["n"].AsInt;
                MoveCount = data["M"].AsInt;
                TurnAngleRadian = data["tu"].AsFloat;
                TurnTimeS = data["ts"].AsFloat;
                NextV = data["nv"].AsFloat;
                NextMoveCount = data["nc"].AsInt;
                NextTurnAngleRadian = data["nr"].AsFloat;
                NextTurnTimeS = data["ns"].AsFloat;
                var Width2 = data["w"].AsFloat;
                var Height2 = data["H"].AsFloat;
                Type = (Config.FishType)data["t"].AsInt;
                direction.X = data["dx"].AsFloat;
                direction.Y = data["dy"].AsFloat;
                turnTimeCount = data["tt"].AsFloat;
                //if (Width2 != Width || Height2 != Height)
                {
                    Width = Width2;
                    Height = Height2;
                    MaxValue = FishFactory.CalculateMaxValue(MaxHealth, world.TableBlindIndex);
                    ForceSetDirection();
                    UpdateBound();
                }
            }
            else
            {
                ID = data["id"].AsInt;
                Pos.ParseJson(data["pos"].AsObject);
                V.ParseJson(data["v"].AsObject);
                A.ParseJson(data["a"].AsObject);
                SpeedUp = data["speedUp"].AsBool;
                Health = data["health"].AsFloat;
                Value = data["value"].AsLong;
                MoveType = (Config.MoveType)data["moveType"].AsInt;
                NextMoveType = (Config.MoveType)data["nextMoveType"].AsInt;
                MoveCount = data["moveCount"].AsInt;
                TurnAngleRadian = data["turnAngleRadian"].AsFloat;
                TurnTimeS = data["turnTimeS"].AsFloat;
                NextV = data["nextV"].AsFloat;
                NextMoveCount = data["nextMoveCount"].AsInt;
                NextTurnAngleRadian = data["nextTurnAngleRadian"].AsFloat;
                NextTurnTimeS = data["nextTurnTimeS"].AsFloat;
                var Width2 = data["width"].AsFloat;
                var Height2 = data["height"].AsFloat;
                Type = (Config.FishType)data["type"].AsInt;
                direction.ParseJson(data["direction"].AsObject);
                turnTimeCount = data["turnTimeCount"].AsFloat;
                //if (Width2 != Width || Height2 != Height)
                {
                    Width = Width2;
                    Height = Height2;
                    MaxValue = FishFactory.CalculateMaxValue(MaxHealth, world.TableBlindIndex);
                    UpdateBound();
                }
            }

            // DEBUG
            //if (!isShadow)
            //{
            //    //UnityEngine.Debug.Log("Server: " + world.IsServer + " change move type " + this.ToJson().ToString());
            //    if (!GameBanCa.histories.ContainsKey(ID))
            //    {
            //        GameBanCa.histories[ID] = new JSONArray();
            //    }
            //    var data2 = ToJson();
            //    data2["isServer"] = world.IsServer;
            //    GameBanCa.histories[ID].Add(data2);
            //}
            followShadow = false;
        }

        internal void ForceUpdate(JSONNode data)
        {
            //Logger.Log("Force update: " + data.ToString());
            // timestamp at server when data is recorded
            var time = data["time"].AsLong;

            if (time <= lastTimeStamp)
                return;
            lastTimeStamp = time;

            // how much time pass when client receive this message (guess)
            var timePass = (TimeUtil.TimeStamp + TimeUtil.ClientServerTimeDifferentMs - time) / 1000f;
            if (Type != (Config.FishType)data[data.HasKey("t") ? "t" : "type"].AsInt || Health <= 0) // change type or revive, set immediately
            {
                this.ParseJson(data);
            }
            else
            {
                if (shadow == null)
                {
                    shadow = new BanCaObject(world);
                    shadow.ID = this.ID;
                    shadow.isShadow = true;
                    shadow.isSpecial = isSpecial;
                }
                followShadow = true;

                shadow.ParseJson(data);

                followTime = AdvanceTime(world, shadow, 200 + timePass) - (time > 0 ? timePass : 0);

                tempV.Set(shadow.Pos.X - Pos.X, shadow.Pos.Y - Pos.Y).Mul(1 / followTime); // fish reach target in "followTime"

                //// DEBUG
                var vS = tempV.SquareLength;
                if (vS > Config.MAX_SHADOW_SPEED_2)
                {
#if !NetCore
                    Logger.Log("move speed: " + MathHaxe.sqrt(tempV.SquareLength) + " follow time " + followTime + " time pass " + timePass);
                    Logger.Log("move too fast: \n" + shadow.ToJson().ToString() + "\nvs \n" + this.ToJson().ToString());
#endif

                    if (vS > Config.TELEPORT_SHADOW_SPEED_2)
                    {
                        this.ParseJson(data);
                        AdvanceTime(world, this, timePass);
                        followShadow = false;
                    }
                    else
                    {
                        // Solution 1: set pos directly on desync
                        //if (vS > 2 * MAX_SHADOW_SPEED_2)
                        //{
                        //    UnityEngine.Debug.Log("too fast, set pos directly");
                        //    this.ParseJson(data);
                        //    AdvanceTime(world, this, timePass);
                        //    followShadow = false;
                        //}

                        // Solution 2: cap max speed, less accuracy but more smooth
                        tempV.Normalize().Mul((float)MathHaxe.sqrt(Config.MAX_SHADOW_SPEED_2));

                        //if (GameBanCa.histories.ContainsKey(ID))
                        //{
                        //    UnityEngine.Debug.Log(GameBanCa.histories[ID].ToString());
                        //}
                        //UnityEditor.EditorApplication.isPaused = true;
                    }
                }

                if (followShadow)
                    V.Set(tempV);
            }
        }

        public static float AdvanceTime(GameBanCa world, BanCaObject obj, float time, float delta = 0.033f)
        {
            var fromTime = time;
            while (time >= 0)
            {
                time -= delta;
                if (time >= 0)
                {
                    obj.Update(delta);

                    if (obj.isSpecial)
                    {
                        obj.CheckBound(Config.OutWorldX, Config.OutWorldY, Config.OutWorldW, Config.OutWorldH);
                    }
                    else
                    {
                        if (world.WorldState != State.WaitingForNewWave) // allow fish to move off screen
                            obj.CheckBound(Config.WorldX, Config.WorldY, Config.WorldW, Config.WorldH);
                    }
                }
                else
                {
                    obj.Update(delta + time);

                    if (obj.isSpecial)
                    {
                        obj.CheckBound(Config.OutWorldX, Config.OutWorldY, Config.OutWorldW, Config.OutWorldH);
                    }
                    else
                    {
                        if (world.WorldState != State.WaitingForNewWave) // allow fish to move off screen
                            obj.CheckBound(Config.WorldX, Config.WorldY, Config.WorldW, Config.WorldH);
                    }
                    time = 0;
                    break;
                }
            }
            return fromTime - time;
        }

        private void copyValue(BanCaObject data)
        {
            //UnityEngine.Debug.Log("Set health the fish by copy: " + ID);
            A.Set(data.A);
            SpeedUp = data.SpeedUp;
            Health = data.Health;
            MoveCount = data.MoveCount;
            MoveType = data.MoveType;
            NextMoveType = data.NextMoveType;
            Pos.Set(data.Pos);
            TurnAngleRadian = data.TurnAngleRadian;
            TurnTimeS = data.TurnTimeS;
            V.Set(data.V);
            if (world.IsServer)
                Value = data.Value;
            Type = data.Type;

            NextV = data.NextV;
            NextMoveCount = data.NextMoveCount;
            NextTurnAngleRadian = data.NextTurnAngleRadian;
            NextTurnTimeS = data.NextTurnTimeS;

            turnTimeCount = data.turnTimeCount;
            direction.Set(data.direction);

            //if (Width != data.Width || Height != data.Height)
            //{
            Width = data.Width;
            Height = data.Height;
            MaxValue = FishFactory.CalculateMaxValue(MaxHealth, world.TableBlindIndex);
            UpdateBound();
            //}
            //else
            //{
            //    BoundingBox.set_x(Pos.X);
            //    BoundingBox.set_y(Pos.Y);
            //}
        }
    }
}
