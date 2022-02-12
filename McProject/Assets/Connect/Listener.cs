using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Threading;
using System.Net.Sockets;

public class Listener : MonoBehaviour {

    public Text Warn;

    /// <summary>
    /// һ�����������������ض�messageʱ������Ϣ��������
    /// </summary>
    public static Dictionary<string, string> waiting = new Dictionary<string, string>();

    /// <summary>
    /// �������ضԷ������ļ���
    /// </summary>
    public static void StartListening() {
        Thread Listening = new Thread(new ThreadStart(Listen));
        Listening.Start();
    }

    static byte[] readBuff = new byte[1024000];
    static void Listen() {//���������߳�
        while(UserClient.isAvailable()) {
            try {
                int count = UserClient.socket.Receive(readBuff);
                string str = System.Text.Encoding.UTF8.GetString(readBuff, 0, count);
                foreach (string s in str.Split('&')) {
                    if(s.Length > 0) {
                        Message recv = JsonConvert.DeserializeObject<Message>(s);
                        //ת�����߳���ִ�м���������Ϣ
                        toDolist.Enqueue(recv);
                        if (waiting.ContainsKey(recv.type))
                            waiting[recv.type] = recv.info;
                    }
                }
            } catch(SocketException) {
                break;
            } catch (System.Exception e) {
                Debug.Log(e.Message);
            }
        }
        Thread.Sleep(1000);
        if(!Title.inTitle) {
            toDolist.Enqueue(new Message("ExitGame", "ը����"));
        }
    }

    /// <summary>
    /// Listener���������Messageͨ���˶���ת����������ִ��
    /// </summary>
    static Queue<Message> toDolist = new Queue<Message>();
    private void Update() {

        if(toDolist.Count > 0) {
            Message recv = toDolist.Dequeue();
            switch(recv.type) {
                case "Logout":
                    PlayerPool.Remove(recv.info);
                    break;
                case "UpdateWorld":
                    //���类�޸�
                    WorldModify wd = JsonConvert.DeserializeObject<WorldModify>(recv.info);
                    World.ins.modify(wd);
                    break;
                case "UpdateAllUser":
                    var listinfo = JsonConvert.DeserializeObject<List<UserInfo>>(recv.info);
                    PlayerPool.Fresh(listinfo);
                    break;
                case "UpdateWorldList":
                    World.ins.clear();
                    List<WorldModify> allmodi = JsonConvert.DeserializeObject<List<WorldModify>>(recv.info);
                    foreach(var i in allmodi) {
                        World.ins.modify(i);
                    }
                    break;
                case "ExitGame":
                    Title.ins.ShowWarn(recv.info);
                    Title.ins.BackToTitle();
                    break;
            }
        }
    }
}