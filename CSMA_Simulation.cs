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
            _generate_timer = _PickGentime();
            _transmit_timer = 0L;
            _backoff_timer = 0L;
            _curr_packet_delay = 0L;
            if (CommManager.Instance.scheme == 2) _cw = 2;
            else _cw = CommManager.Instance.CW;
            _timeout_added = false;
        }

        private NodeState _state;

        private long _generate_timer;
        private long _transmit_timer;
        private long _backoff_timer;
        private long _curr_packet_delay;
        private long _cw;

        private bool _timeout_added; // Only for CSMA w/o CD

        private long _PickGentime()
        {
            double ln_x = Math.Log(rand.NextDouble());
            long gentime;

            try
            {
                gentime = Convert.ToInt64(-10000.0 * ln_x);
            }
            catch (OverflowException _)
            {
                gentime = Int64.MaxValue;
            }

            if (gentime == 0L) gentime = 1L;

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
                case NodeState.Transmit:
                    if(manager.scheme == 0 && manager.collision_trigger == true) // CSMA
                    {
                        if(_timeout_added == false)
                        {
                            _transmit_timer += (_cw / 16L) * 50L; // (timeout timer) = (CW / 16) * (slot time)
                            _timeout_added = true;
                        }
                        else if(_transmit_timer == 0L) // ACK를 기다리다 timeout된 상황
                        {
                            _state = NodeState.Backoff;
                            _backoff_timer = (rand.Next(1, Convert.ToInt32(_cw) + 1) * 50);

                            manager.n_collided_request++;
                            manager.next_collision_trigger = false;
                            manager.medium_next_state = MediumState.Idle;
                            _timeout_added = false;
                        }
                    }
                    else if(manager.collision_trigger == true) // CSMA/CD
                    {
                        _transmit_timer = 0L;

                        _state = NodeState.Backoff;
                        _backoff_timer = (rand.Next(1, Convert.ToInt32(_cw) + 1) * 50);
                        if (manager.scheme == 2 && _cw < 1024L) // CSMA/CD with binary exponential backoff
                            _cw = 2L * _cw;

                        manager.n_collided_request++;
                        manager.next_collision_trigger = false;
                        manager.medium_next_state = MediumState.Idle;
                    }
                    else if (_transmit_timer == 0L) // 성공적인 전송
                    {
                        _state = NodeState.Generate;
                        _generate_timer = _PickGentime();

                        manager.sum_packet_delay += _curr_packet_delay;
                        manager.n_successful_packet++;

                        if (manager.scheme == 2)
                            _cw = 2L;

                        _curr_packet_delay = 0L;
                        manager.medium_next_state = MediumState.Idle;
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
                manager.n_request++;
                if (manager.next_collision_trigger == false && manager.medium_next_state == MediumState.Busy) 
                    manager.next_collision_trigger = true;
                manager.medium_next_state = MediumState.Busy;
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
            _node_list = new List<CommNode>();
        }

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

        // Needed fields
        private List<CommNode> _node_list;
        public StreamWriter output_file = null;

        public MediumState medium_state;
        public MediumState medium_next_state;
        public bool collision_trigger;
        public bool next_collision_trigger;

        // parameters
        public int scheme;
        public long CW;
        public long node_num;
        public long process_time;

        // simulation result variables
        public long n_successful_packet;
        public long sum_packet_delay;
        public long n_request;
        public long n_collided_request;

        public long elapsed_time;

        // mesurment properties
        public double Throughput
        {
            get
            {
                double elapsed_time_sec = elapsed_time / 1000000.0;
                return n_successful_packet / elapsed_time_sec;
            }
        }
        public double MeanPacketDelay
        {
            get
            {
                return sum_packet_delay / Convert.ToDouble(n_successful_packet);
            }
        }
        public double CollisionProb
        {
            get
            {
                return n_collided_request / Convert.ToDouble(n_request);
            }
        }


        public void PrintCurrentState(StreamWriter file=null)
        {
            if (file == null) // for debugging, pretty-print to console.
            {
                Console.WriteLine("  Result at {0} ms:", elapsed_time / 1000.0);
                Console.WriteLine("    Throughput:            {0,10} packet/sec", Throughput);
                Console.WriteLine("    Mean Packet Delay:     {0,10} us", MeanPacketDelay);
                Console.WriteLine("    Collision Probablity:  {0,10} %", CollisionProb * 100.0);
                Console.WriteLine("|--------------------------------------------------------------------------|");
            }
            else
            {
                file.WriteLine("{0} {1} {2}", Throughput, MeanPacketDelay, CollisionProb);
            }
            
        }

        public void ResetManager()
        {
            n_successful_packet = 0L;
            n_request = 0L;
            n_collided_request = 0L;
            sum_packet_delay = 0L;

            process_time = 0L;
            elapsed_time = 0L;

            medium_state = MediumState.Idle;

            _node_list.Clear();
        }

        public void ChangeScheme(int new_scheme)
        {
            scheme = new_scheme;
        }

        public void ChangeParameters(long new_cw, long new_node_num)
        {
            CW = new_cw;
            node_num = new_node_num;
        }

        public void Simulate(long period_sec)
        {
            ResetManager();

            process_time = period_sec * 1000000L;
            for(long i = 0L; i < node_num; i++)
            {
                _node_list.Add(new CommNode());
            }

            for(long t = 0L; t < process_time; t++)
            {
                medium_next_state = medium_state;
                next_collision_trigger = collision_trigger;

                // 노드별 시간 진행 및 스테이트 조절
                foreach(CommNode node in _node_list)
                {
                    node.ProcessNode();
                }
                // 매체 상태 조절
                medium_state = medium_next_state;
                collision_trigger = next_collision_trigger;

                elapsed_time++;

                // 상태 출력
                if (elapsed_time % 100000L == 0)
                    PrintCurrentState(output_file);
            }
            Console.WriteLine("Done.");
        }
    }

    public class CSMA_Simulation
    {
        static void Main(string[] args)
        {
            CommManager m = CommManager.Instance;

            if(args.Length != 0 && args[0].Equals(""))
            {
                string windir = System.Environment.GetEnvironmentVariable("windir");
                m.output_file = new StreamWriter(windir + "\\" + args[0]);
            }

            m.ChangeScheme(0);
            m.ChangeParameters(32L, 25L);

            m.Simulate(1000L);
        }
    }
}
