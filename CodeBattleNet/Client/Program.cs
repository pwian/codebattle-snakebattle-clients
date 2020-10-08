using System;
using System.Collections.Generic;
using System.Linq;
using SnakeBattle.Api;

namespace Client
{
    class Program
    {
        const string SERVER_ADDRESS = "http://codergames.northeurope.cloudapp.azure.com/codenjoy-contest/board/player/3axfqd38ebiitunu9xnd?code=6178561518884758672&gameName=snakebattle";
        const int MaxEvelRounds = 10;

        static Direction prevDirection = Direction.Right;
        static int restEvelRound = 0;

        static BoardElement[] enemyHeads => new[] {
            BoardElement.EnemyHeadDown,
            BoardElement.EnemyHeadLeft,
            BoardElement.EnemyHeadRight,
            BoardElement.EnemyHeadUp,
            BoardElement.EnemyHeadEvil,
            BoardElement.EnemyHeadFly,
            BoardElement.EnemyHeadSleep,
        };

        static KeyValuePair<BoardElement, Func<bool>>[] elementsByFunc = new KeyValuePair<BoardElement, Func<bool>>[] {
            new KeyValuePair<BoardElement, Func<bool>>(BoardElement.Stone, () => restEvelRound > 1 ),
            new KeyValuePair<BoardElement, Func<bool>>(BoardElement.EnemyHeadRight, () => true ),
            new KeyValuePair<BoardElement, Func<bool>>(BoardElement.EnemyHeadUp, () => true ),
            new KeyValuePair<BoardElement, Func<bool>>(BoardElement.EnemyHeadDown, () => true ),
            new KeyValuePair<BoardElement, Func<bool>>(BoardElement.EnemyHeadLeft, () => true ),
            new KeyValuePair<BoardElement, Func<bool>>(BoardElement.EnemyHeadEvil, () => false ),
        };

        static BoardElement[] strongBadElements => new[] { 
            BoardElement.Wall,
            BoardElement.StartFloor,
            BoardElement.EnemyHeadSleep,
            BoardElement.EnemyTailInactive,
            BoardElement.TailInactive,
            BoardElement.EnemyHeadDead,
        }
        .Union(
                elementsByFunc
                .Where(pair => !pair.Value())
                .Select(pair => pair.Key))
            .ToArray();

        static BoardElement[] badElements => strongBadElements.Union(
            new[] {
                BoardElement.HeadEvil,
                BoardElement.HeadRight,
                BoardElement.HeadUp,
                BoardElement.HeadDown,
                BoardElement.HeadLeft,
                BoardElement.BodyHorizontal,
                BoardElement.BodyVertical,
                BoardElement.BodyLeftDown,
                BoardElement.BodyLeftUp,
                BoardElement.BodyRightDown,
                BoardElement.BodyRightUp,
                BoardElement.TailEndDown,
                BoardElement.TailEndLeft,
                BoardElement.TailEndUp,
                BoardElement.TailEndRight,
                BoardElement.TailInactive 
            })
            .Union(
                elementsByFunc
                .Where(pair => !pair.Value())
                .Select(pair => pair.Key))
            .ToArray();

        static BoardElement[] goodElements => new[] {
            BoardElement.Apple,
            BoardElement.FlyingPill,
            BoardElement.Gold,
            BoardElement.FuryPill,
            BoardElement.EnemyBodyHorizontal,
            BoardElement.EnemyBodyVertical,
            BoardElement.EnemyBodyLeftDown,
            BoardElement.EnemyBodyLeftUp,
            BoardElement.EnemyBodyRightDown,
            BoardElement.EnemyBodyRightUp,
            BoardElement.EnemyTailEndDown,
            BoardElement.EnemyTailEndLeft,
            BoardElement.EnemyTailEndUp,
            BoardElement.EnemyTailEndRight,
            BoardElement.EnemyTailInactive
        }
        .Union(elementsByFunc
                .Where(pair => pair.Value())
                .Select(pair => pair.Key))
        .ToArray();

        static bool canRight(GameBoard gameBoard, BoardPoint head) => prevDirection != Direction.Left &&
                !badElements.Contains(gameBoard.GetElementAtOrWall(head.ShiftRight()));
        static bool canUp(GameBoard gameBoard, BoardPoint head) => prevDirection != Direction.Down &&
                !badElements.Contains(gameBoard.GetElementAtOrWall(head.ShiftTop()));
        static bool canDown(GameBoard gameBoard, BoardPoint head) => prevDirection != Direction.Up &&
                !badElements.Contains(gameBoard.GetElementAtOrWall(head.ShiftBottom()));
        static bool canLeft(GameBoard gameBoard, BoardPoint head) => prevDirection != Direction.Right &&
                !badElements.Contains(gameBoard.GetElementAtOrWall(head.ShiftLeft()));

        static int incrementIfNotBadPoint(GameBoard gameBoard, BoardPoint point) => strongBadElements.Contains(gameBoard.GetElementAtOrWall(point)) ? 0 : 1;

        static bool isNotDeadEnd(GameBoard gameBoard, BoardPoint point) => 
                incrementIfNotBadPoint(gameBoard, point.ShiftRight()) +
                incrementIfNotBadPoint(gameBoard, point.ShiftTop()) +
                incrementIfNotBadPoint(gameBoard, point.ShiftBottom()) +
                incrementIfNotBadPoint(gameBoard, point.ShiftLeft())
            > 1;

        static int incrementIfNotDeadEnd(GameBoard gameBoard, BoardPoint point) => incrementIfNotBadPoint(gameBoard, point) == 1 && isNotDeadEnd(gameBoard, point) ? 1 : 0;

        static bool isNotDeadEnd2(GameBoard gameBoard, BoardPoint point) =>
            incrementIfNotDeadEnd(gameBoard, point.ShiftRight()) +
            incrementIfNotDeadEnd(gameBoard, point.ShiftTop()) +
            incrementIfNotDeadEnd(gameBoard, point.ShiftBottom()) +
            incrementIfNotDeadEnd(gameBoard, point.ShiftLeft())
            > 1;

        private static Direction GetDefaultDirection(GameBoard gameBoard, BoardPoint head)
        {
            if (head == null)
            {
                return Direction.Right;
            }

            if (prevDirection != Direction.Left &&
                !strongBadElements.Contains(gameBoard.GetElementAtOrWall(head.ShiftRight())))
            {
                return Direction.Right;
            }

            if (prevDirection != Direction.Down &&
                !strongBadElements.Contains(gameBoard.GetElementAtOrWall(head.ShiftTop())))
            {
                return Direction.Up;
            }

            if (prevDirection != Direction.Up &&
                !strongBadElements.Contains(gameBoard.GetElementAtOrWall(head.ShiftBottom())))
            {
                return Direction.Down;
            }

            return Direction.Left;
        }

        private static Direction GetDirectionToTarget(GameBoard gameBoard, BoardPoint head, BoardPoint target)
        {
            if (canRight(gameBoard, head) && head.X < target.X)
            {
                return Direction.Right;
            }

            if (canUp(gameBoard, head) && head.Y > target.Y)
            {
                return Direction.Up;
            }

            if (canDown(gameBoard, head) && head.Y < target.Y)
            {
                return Direction.Down;
            }

            if (canLeft(gameBoard, head) && head.X > target.X)
            {
                return Direction.Left;
            }

            return GetDefaultDirection(gameBoard, head);
        }

        private static bool IsEnemyInTheTail(GameBoard gameBoard)
        {
            var myTailPoint = gameBoard.GetMyTail();
            if (myTailPoint == null)
            {
                return false;
            }

            return gameBoard.HasElementAt(myTailPoint.Value.ShiftRight(), enemyHeads) ||
                gameBoard.HasElementAt(myTailPoint.Value.ShiftTop(), enemyHeads) ||
                gameBoard.HasElementAt(myTailPoint.Value.ShiftBottom(), enemyHeads) ||
                gameBoard.HasElementAt(myTailPoint.Value.ShiftLeft(), enemyHeads);
        }

        static void Main(string[] args)
        {
            var client = new SnakeBattleClient(SERVER_ADDRESS);
            client.Run(DoRun);

            Console.ReadKey();
            client.InitiateExit();
        }

        private static SnakeAction DoRun(GameBoard gameBoard)
        {
            if (!gameBoard.AmIEvil())
            {
                restEvelRound = 0;
            }
            else if (restEvelRound == 0)
            {
                restEvelRound = MaxEvelRounds;
            }

            if (restEvelRound > 0)
            {
                restEvelRound--;
            }


            var head = gameBoard.GetMyHead();
            if (head == null)
            {
                prevDirection = Direction.Right;
                return new SnakeAction(IsEnemyInTheTail(gameBoard), prevDirection);
            }

            var huntingElements = gameBoard.FindAllElements(goodElements);
            var hunting = huntingElements
                .OrderBy(element => Math.Abs(element.X - head.Value.X) + Math.Abs(element.Y - head.Value.Y))
                .FirstOrDefault(element => isNotDeadEnd2(gameBoard, element));
            if (hunting == null)
            {
                prevDirection = GetDefaultDirection(gameBoard, head.Value);
                return new SnakeAction(IsEnemyInTheTail(gameBoard), prevDirection);
            }

            prevDirection = GetDirectionToTarget(gameBoard, head.Value, hunting);
            return new SnakeAction(IsEnemyInTheTail(gameBoard), prevDirection);
        }        
    }
}