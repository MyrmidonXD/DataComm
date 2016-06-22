using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSMA_Simulation
{
    public enum NodeState { Generate, Sense, Transmit, Backoff }
    public enum MediumState { Idle, Busy }

    public class CommNode
    {
        public static Random rand = new Random(); // For random number generation.

        public CommNode()
        {
            _state = NodeState.Generate;
            _generate_timer = 0L;
            _transmit_timer = 0L;
            _backoff_timer = 0L;
            _curr_packet_delay = 0L;
            _cw = CommManager.Instance.CW;
        }

        private NodeState _state;

        private long _generate_timer;
        private long _transmit_timer;
        private long _backoff_timer;
        private long _curr_packet_delay;
        private long _cw;

        private long _PickGentime()
        {
            double ln_x = Math.Log(rand.NextDouble());
            long gentime;

            try
            {
                gentime = Convert.ToInt64(-10000.0 * ln_x);
            }
            catch (OverflowException e)
            {
                gentime = Int64.MaxValue;
            }

            return gentime;
        }

        public void ProcessNode()
        {
            CommManager manager = CommManager.Instance;

            //------------------------------- State Transition --------------------------------------
            switch (_state)
            {
                case NodeState.Generate:
                    if (_generate_timer == 0L) // 패킷 생성 후 전송 시도
                    {
                        _state = NodeState.Sense;
                        _curr_packet_delay = 0L; // 패킷 전송에 걸린 시간 측정을 시작한다
                    }
                    break;
                /*
                 case NodeState.Sense: // Process after other state transition occurs.
                     break;
                 */
                case NodeState.Transmit:
                    // TODO
                    //   if (collision_happen) {
                    //       _state = NodeState.Backoff;
                    //       _backoff_timer = [1, _cw] * 50L;
                    //   }
                    if (_transmit_timer == 0L) // 성공적인 전송
                    {
                        _state = NodeState.Generate;
                        _generate_timer = _PickGentime();
                    }
                    break;
                case NodeState.Backoff:
                    if (_backoff_timer == 0L) // Backoff 끝 -> 재전송 시도
                    {
                        _state = NodeState.Sense;
                    }
                    break;
            }

            if(_state == NodeState.Sense && manager.medium_state == MediumState.Idle) // 매체 Idle이라 전송 진행
            {
                _state = NodeState.Transmit;
                _transmit_timer = 800L;
            }

            //------------------------------- Timer Processing --------------------------------------
            switch (_state)
            {
                case NodeState.Generate:
                    _generate_timer--;
                    break;
                case NodeState.Sense:
                    _curr_packet_delay++;
                    break;
                case NodeState.Transmit:
                    _transmit_timer--;
                    _curr_packet_delay++;
                    break;
                case NodeState.Backoff:
                    _backoff_timer--;
                    _curr_packet_delay++;
                    break;
            }

        }
    }

    public class CommManager
    {
        private CommManager()
        {
            medium_state = MediumState.Idle;
        }

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

        public MediumState medium_state;

        public int scheme;
        public long CW;


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

    public class CSMA_Simulation
    {
        static void Main(string[] args)
        {
        }
    }
}
