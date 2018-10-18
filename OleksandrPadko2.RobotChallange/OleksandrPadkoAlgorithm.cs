using Robot.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OleksandrPadko2.RobotChallange
{
    public class OleksandrPadkoAlgorithm : IRobotAlgorithm
    {
        int mCurrentRound = 0;

        public OleksandrPadkoAlgorithm()
        {
            Logger.OnLogRound += Logger_OnRound;
        }

        void Logger_OnRound(object sender, LogRoundEventArgs a)
        {
            mCurrentRound++;
        }

        public string Author
        {
            get
            {
                return "Oleksandr Padko";
            }
        }
        public string Description
        {
            get
            {
                return "dunno";
            }
        }

        private bool isNearStation(Position pos, Map map)
        {
            var nearestStations = map.GetNearbyResources(pos, 2);
            return nearestStations.Count() != 0;
        }

        private bool isFarmer(Robot.Common.Robot robot, Map map)
        {
            foreach(var station in map.GetNearbyResources(robot.Position, 2))
            {
                if(station.Energy >= station.RecoveryRate)
                {
                    return true;
                }
            }
            return false;
        }

        private List<EnergyStation> getFreeStations(int radius, IList<Robot.Common.Robot> robots, int robotToMoveIndex, Map map)
        {
            var stations = map.GetNearbyResources(robots[robotToMoveIndex].Position, radius);
            return stations.Where(station =>
            {
                foreach(var robot in robots)
                {
                    int dist = Math.Max(Math.Abs(robot.Position.X - station.Position.X),
                                        Math.Abs(robot.Position.Y - station.Position.Y));
                    if(dist <= 2)
                    {
                        return false;
                    }
                }
                return true;
            }).ToList();
        }

        int energyToMove(Position pos1, Position pos2)
        {
            return (int)Math.Pow(pos1.X - pos2.X, 2) + (int)Math.Pow(pos1.Y - pos2.Y, 2);
        }

        private RobotCommand moveToBestStation(List<EnergyStation> stations, IList<Robot.Common.Robot> robots, int robotToMoveIndex, Map map)
        {
            var robot = robots[robotToMoveIndex];
            int maxMoves = 10;
            stations.OrderBy(x => energyToMove(robot.Position, x.Position));

            foreach (var station in stations)
            {

                for (int moves = 1; moves <= maxMoves; moves++)
                {
                    Position step = robot.Position.Copy();
                    step.X = (step.X * (moves - 1) + station.Position.X) / moves;
                    step.Y = (step.Y * (moves - 1) + station.Position.Y) / moves;

                    int totalEnergy = energyToMove(robot.Position, step) * moves;
                    if (totalEnergy <= robot.Energy)
                    {
                        return new MoveCommand() { NewPosition = step };
                    }
                }
            }
            return new MoveCommand();
        }

        private RobotCommand DoStepHunter(IList<Robot.Common.Robot> robots, int robotToMoveIndex, Map map)
        {
            // Find best empty energy or energy with a fat competitor
            var robot = robots[robotToMoveIndex];
            //Log(robot.Owner.Name);
            int radius = 40;

            if(getFreeStations(2, robots, robotToMoveIndex, map).Count == 0 &&
               map.GetNearbyResources(robot.Position, 2).Count != 0)
            {
                var enemies = robots.Where(rob => rob.Owner.Name != Author)
                                    .ToList();
                enemies.OrderBy(rob => energyToMove(rob.Position, robot.Position));
                foreach(var enemy in enemies)
                {
                    if(enemy.Energy * 0.1 > 50 + energyToMove(robot.Position, enemy.Position))
                    {
                        return new MoveCommand() { NewPosition = enemy.Position };
                    }
                }
            }

            var freeStations = getFreeStations(radius, robots, robotToMoveIndex, map);
            if (freeStations.Count >= 1)
            {
                return moveToBestStation(freeStations, robots, robotToMoveIndex, map);
            }

            var stations = map.GetNearbyResources(robot.Position, radius)
                           .Where(station => {
                               foreach(var rob in robots)
                               {
                                   if(rob.Owner.Name == Author)
                                   {
                                       if (Math.Max(Math.Abs(rob.Position.X - station.Position.X), Math.Abs(rob.Position.Y - station.Position.Y)) <= 2) {
                                           return false;
                                       }
                                   }
                                   
                               }
                               return true;
                           }).ToList();
            if(stations.Count >= 1)
            {
                return moveToBestStation(stations, robots, robotToMoveIndex, map);
            }

            return new CollectEnergyCommand();
        }

        private RobotCommand DoStepFarmer(IList<Robot.Common.Robot> robots, int robotToMoveIndex, Map map)
        {
            // Farm or create a new one
            var robot = robots[robotToMoveIndex];
            Random rnd = new Random();
            if (mCurrentRound < 35 && robot.Energy > 400 && getFreeStations(40, robots, robotToMoveIndex, map).Count > 0)
            {
                return new CreateNewRobotCommand() { NewRobotEnergy = 100 };
            }
            return new CollectEnergyCommand();
        }

        public RobotCommand DoStep(IList<Robot.Common.Robot> robots, int robotToMoveIndex, Map map)
        {
            //Logger.LogMessage(new Owner() { Name = Author, Algorithm = this }, "yo");
            Log("bleat");

            if (isFarmer(robots[robotToMoveIndex], map))
            {
                return DoStepFarmer(robots, robotToMoveIndex, map);
            }
            else
            {
                return DoStepHunter(robots, robotToMoveIndex, map);
            }
        }


        private void Log(string msg)
        {
            using (var sw = new StreamWriter("log.txt", true))
            {
                sw.WriteLine($"[{DateTime.Now.ToLongTimeString()}]:${msg}");
            }
        }
    }
}
