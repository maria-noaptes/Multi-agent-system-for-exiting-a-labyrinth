using ActressMas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Reactive
{
    public class ExplorerAgent : Agent
    {
        private int _x, _y;
        private int x_end, y_end;
        private bool deadEnd = false;
        private const int deadEndWeight = 5;
        private Dictionary<string, int> toRemember = new Dictionary<string, int>();
        private string positionExit = null;
        private List<string> pathExit = null;  // Dijkistra path
        private int nrMoves = 0;

        string[] start = Utils.start.Split();

        public override void Setup()
        {
            Console.WriteLine("Starting " + Name);
            _x = Int32.Parse(start[0]);
            _y = Int32.Parse(start[1]);

            string[] end = Utils.end.Split();
            x_end = Int32.Parse(end[0]);
            y_end = Int32.Parse(end[1]);

            Send("planet", Utils.Str("position", _x, _y));
        }
        public void restart()
        {
            Console.WriteLine("Restarting " + Name);
            deadEnd = false;
            toRemember.Clear();
            positionExit = null;
            pathExit = null;
            nrMoves = 0;

            _x = Int32.Parse(start[0]);
            _y = Int32.Parse(start[1]);

            Send("planet", Utils.Str("position", _x, _y));
        }

       public bool isOccupied(string newPos, List<string> occupied)
        {
            if (occupied.Contains(newPos)) return true;
            else return false;
        }

       public override void Act(Message message)
        {
            Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

            string action;
            List<string> parameters;
            Utils.ParseMessage(message.Content, out action, out parameters);
         
            switch (action)
            {
                case "move":
                    if (!(IsAtExit()))
                    {
                        string mess;
                        if (positionExit == null)
                        {
                            MoveRandomly(parameters);

                            mess = Utils.Str("change", _x, _y);

                            int weight = 1;
                            if (deadEnd) weight = deadEndWeight;

                            for (int i = 0; i < Utils.NoExplorers; i++)
                                Send("explorer" + i, mess + " " + weight);
                        }
                        else
                        {
                            if (pathExit == null) {
                                computeShortestPath();
                            } 
                            // go to the exit
                            int index = pathExit.IndexOf(_x + " " + _y);

                            if (!isOccupied((pathExit[index - 1]).Replace(" ", "-"), parameters)) 
                                move(pathExit[index - 1]);

                            mess = Utils.Str("change", _x, _y);
                        }
                        Send("planet", mess);
                        nrMoves++;
                    }
                    else
                    {
                        if (positionExit == null)
                        {
                            positionExit = _x + " " + _y;
                            for (int i = 0; i < Utils.NoExplorers; i++)
                                if (this.Name != "explorer" + i)
                                {
                                    Send("explorer" + i, Utils.Str("winner", _x, _y));
                                }
                        }
                        Send("planet", Utils.Str("winner", nrMoves));
                       // Stop();
                    }
                    break;

               case "change":
                    // message from other explorers
                    visit(string.Join(" ", parameters.Take(2)), Int32.Parse(parameters[2]));
                    break;

               case "winner":
                    positionExit = string.Join(" ", parameters);
                    visit(positionExit, 1); // put the node in the toRemember dictionary
                    break;

                case "restart":
                    restart();
                    break;

                default:
                    break;
            }

        }

        private void computeShortestPath()
        {
            // find shortest path Dijkistra
            int v = toRemember.Keys.Count;

            Dictionary<string, List<string>> adj =
                       new Dictionary<string, List<string>>(v);

            foreach (string cell in toRemember.Keys)
            {
                adj[cell] = new List<string>();
            }

            foreach (string cell_1 in toRemember.Keys)
            {
                foreach (string cell_2 in toRemember.Keys)
                {
                    string[] cell1 = cell_1.Split();
                    int x1 = Int32.Parse(cell1[0]);
                    int y1 = Int32.Parse(cell1[1]);

                    string[] cell2 = cell_2.Split();
                    int x2 = Int32.Parse(cell2[0]);
                    int y2 = Int32.Parse(cell2[1]);

                    if ((Math.Abs(x1 - x2) == 1 && y1 == y2) || (Math.Abs(y1 - y2) == 1 && x1 == x2))
                        Utils.addEdge(adj, cell_1, cell_2);
                }
            }
            string source = _x + " " + _y;
            string dest = positionExit;
            Utils.shortestDistance(adj, source, dest, v, out pathExit);
        }

        private void visit(string position, int weight)
        {
            // keep a map of the visited nodes
            if (!(toRemember.Keys.Contains(position)))
            {
                toRemember.Add(position, weight);
            }
            else
            {
                toRemember[position] = toRemember[position] + weight;
            }

        }

        private void move(string position)
        {
            string[] pos = position.Split();
            _x = Int32.Parse(pos[0]);
            _y = Int32.Parse(pos[1]);
        }

        private void MoveRandomly(List<string> occupied)
        {
            int countWallsAroundMe = 0;

            Dictionary<int, string> paths = new Dictionary<int, string>();

            int newX = _x - 1;
            bool floorLeft = _x > 0 && (Utils.maze[newX, _y] == 0);
            if (floorLeft && !isOccupied(newX + "-" + _y, occupied))
            {
                // go left
                paths.Add(0, newX + " " + _y);
            }
            if (!floorLeft) countWallsAroundMe++;

            int newXX = _x + 1;
            bool floorRight = _x < Utils.maze.GetLength(0) - 1 && (Utils.maze[newXX, _y] == 0);
            if (floorRight && !isOccupied(newXX + "-" + _y, occupied))
            {
                // go right
                paths.Add(1, newXX + " " + _y);
            } 
            if (!floorRight) countWallsAroundMe++;

            int newY = _y - 1;
            bool floorUp = _y > 0 && (Utils.maze[_x, newY] == 0);
            if (floorUp && !isOccupied(_x + "-" + newY, occupied))
            {
                // go up
                paths.Add(2, _x + " " + newY);
            } 
            if (!floorUp) countWallsAroundMe++;

            int newYY = _y + 1;
            bool floorDown = _y < Utils.maze.GetLength(1) - 1 && (Utils.maze[_x, newYY] == 0);
            if (floorDown && !isOccupied(_x + "-" + newYY, occupied))
            {
                // go down
                paths.Add(3, _x + " " + newYY);
            }
            if (!floorDown) countWallsAroundMe++;

            if (countWallsAroundMe == 3)
            {
                deadEnd = true;
            }
            if (deadEnd && countWallsAroundMe == 1)
            {
                deadEnd = false;
            }

            string finalPosition = _x + " " + _y;

            if (paths.Count > 0)
            {
                int minimum = int.MaxValue;

                foreach (KeyValuePair<int, string> path in paths)
                {
                    int weight;
                    // for every cell in the paths get toRemember weight
                    if (toRemember.Keys.Contains(path.Value))
                    {
                        weight = toRemember[path.Value];
                    }
                    else weight = 0;
                    if (weight < minimum)
                    {
                        minimum = weight;
                        finalPosition = path.Value;
                    }
                }
                int toAddWeigth = 1;
                if (deadEnd) {
                    toAddWeigth = deadEndWeight; 
                }
                visit(finalPosition, toAddWeigth);

                move(finalPosition);
            }

        }
        private bool IsAtExit()
        {
            return (_x == x_end && _y == y_end); // the position of the base
        }
    }
}