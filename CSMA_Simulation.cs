using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSMA_Simulation
{
    public enum NodeState { Generate, Sense, Transmit, Backoff }

    public class CommNode
    {
        public CommNode()
        {
            _state = NodeState.Generate;
        }

        private NodeState _state;

        private long _generate_timer;

    }

    public class CommManager
    {
        private CommManager() { }

        private List<CommNode> _node_list;
        private static CommManager _instance;
        public static CommManager Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new CommManager();
                }
                return _instance;
            }
        }


        public void PrintCurrentState()
        { // TODO
        }

        public bool ResetSetting()
        { // TODO
            return false;
        }

        public void Simulate(long period_sec)
        { // TODO
        }

        public 




    }

    public class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
