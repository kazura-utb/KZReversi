﻿//! @file
//! アプリケーションフォーム
//****************************************************************************
//       (c) COPYRIGHT kazura_utb 2016-  All Rights Reserved.
//****************************************************************************
// FILE NAME     : Form1.cs
// PROGRAM NAME  : KZReversi
// FUNCTION      : フォーム
//
//****************************************************************************
//****************************************************************************
//
//****************************************************************************
//┌──┬─────┬──────────────────┬───────┐
//│履歴│   DATE   │              NOTES                 │     SIGN     │
//├──┼─────┼──────────────────┼───────┤
//│    │          │                                    │              │
//├──┼─────┼──────────────────┼───────┤
//│ A  │2016/02/01│新規作成                            │kazura_utb    │
//└──┴─────┴──────────────────┴───────┘
//****************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KZreversi
{
    public partial class Form1 : Form
    {

        private BufferedPanel panel1;
        private BufferedPanel panel_back;
        private BufferedPanel panel_board;

        public CppWrapper cppWrapper;
        public bool loadResult;

        private Bitmap bkImg;
        private Bitmap whImg;

        private BoardClass boardclass;
        private CpuClass[] cpuClass;

        private int EVAL_THRESHOLD = 1024;

        private const int ON_NOTHING = 0;
        private const int ON_GAME = 1;
        private const int ON_EDIT = 3;
        private const int ON_HINT = 4;
        private const int ON_HINT_FINISH = 5;

        private const int TURN_HUMAN = 0;
        private const int TURN_CPU = 1;

        private const int BOARD_SIZE = 8;

        private const int COLOR_BLACK = 0;
        private const int COLOR_WHITE = 1;

        private const int BATTLE_MODE = 0;
        private const int ANALYZE_MODE = 1;

        private const int INFINITY_SCORE = 2500000;

        private uint[] dcTable = { 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26 };

        private int m_mode = ON_NOTHING;
        private int m_hint_mode = ON_NOTHING;
        private bool m_abort = false;

        private uint nowColor = COLOR_BLACK;
        private Player nowPlayer;
        private Player[] playerArray;

        private int m_passCount;

        private const ulong MOVE_PASS = 0;

        // ヒント表示用
        private uint m_hintLevel;
        private List<int[]> m_hintList = null;
        private int m_hintEvalMax;

        private Stopwatch m_sw;

        private GCHandle m_gcHandle_setCpuMessageDelegate;
        private GCHandle m_gcHandle_setPVLineDelegate;
        private GCHandle m_gcHandle_setMPCInfoDelegate;

        public delegate void SetMoveProperty(ulong moves);
        public delegate void SetNodeCountProperty(ulong nodeCount);
        public delegate void DoHintProperty(HintClass evalList);
        public delegate void SetCpuMessageProperty(string cpuMsg);
        public delegate void SetMPCInfoProperty(string mpcMsg);

        public SetMoveProperty delegateObj;
        public SetNodeCountProperty nodeCountDelegate;
        public DoHintProperty hintDelegate;
        public SetCpuMessageProperty cpuMessageDelegate;
        public SetCpuMessageProperty setPVLineDelegate;
        public SetCpuMessageProperty setMPCInfoDelegate;

        delegate void SetPVLineDelegate(string text);

        Font m_ft;
        Font m_ft2;
        Font m_ft3;
        Font m_ft4;

        public int m_event;

        private IntPtr cpuMessageDelegatePtr;
        private IntPtr setPVLineDelegatePtr;
        private IntPtr setMPCInfoDelegatePtr;

        private float m_scale;
        private float m_mass_size;
        private float m_fix_x = 0, m_fix_y = 0;
        private float m_board_width, m_board_height;
        private float m_board_start_x, m_board_start_y;
        private const float border_rate = (float)(290.0 / 2450.0);
        private Bitmap m_panel1_bitmap;
        private Bitmap m_back_bitmap;

        private bool _m_hintFlag;
        private bool m_enablePVdisplay;
        private bool m_enableDetailDisplay;
        private bool m_enableResultDisplay;


        private bool m_firsthint;
        GameThread hintGmt;

        private bool _m_cpuFlag;
        private int g_num;
        private int m_game_mode;

        public Form1()
        {
            boardclass = new BoardClass();

            cpuClass = new CpuClass[2];
            cpuClass[0] = new CpuClass(); // BLACK
            cpuClass[1] = new CpuClass(); // WHITE

            delegateObj = new SetMoveProperty(setMove);
            nodeCountDelegate = new SetNodeCountProperty(setNodeCount);
            cpuMessageDelegate = new SetCpuMessageProperty(SetCpuMessage);
            setPVLineDelegate = new SetCpuMessageProperty(SetPVLine);
            setMPCInfoDelegate = new SetCpuMessageProperty(SetMPCInfo);
            hintDelegate = new DoHintProperty(doHintProcess);

            boardclass.InitBoard(COLOR_BLACK);

            InitializeComponent();
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 3;

            // プレイヤー情報を初期化
            playerArray = new Player[2];
            playerArray[COLOR_BLACK] = new Player(Player.PLAYER_HUMAN, COLOR_BLACK);
            playerArray[COLOR_WHITE] = new Player(Player.PLAYER_CPU, COLOR_WHITE);

            // デフォルトプレイヤー
            nowPlayer = playerArray[COLOR_BLACK];
            if (nowColor == COLOR_BLACK)
            {
                label1.BackColor = Color.PowderBlue;
                label2.BackColor = Control.DefaultBackColor;
            }
            else
            {
                label2.BackColor = Color.PowderBlue;
                label1.BackColor = Control.DefaultBackColor;
            }

            // 探索時間表示用
            m_sw = new Stopwatch();

            // ヒント表示用
            m_hintList = new List<int[]>();

            //listBox1.Items.Add(m_mode.ToString());
            // listBox1.Items.Add(m_hint_mode.ToString());
            m_firsthint = true; 
        }

        private void 終了ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // デバッグ作業で煩わしいため一旦コメントアウト
            //if (MessageBox.Show(
            //"終了してもいいですか？", "確認",
            //MessageBoxButtons.YesNo, MessageBoxIcon.Question
            //  ) == DialogResult.No)
            //{
            //    e.Cancel = true;
            //}
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // リソースの解放
            m_ft.Dispose();
            m_ft2.Dispose();
            m_ft3.Dispose();
            if (cppWrapper.GetIsAbort())
            {
                cppWrapper.SendAbort();
            }
            cppWrapper.ReleaseHash();
            cppWrapper.ReleaseBook();
        }

        private void buttonClick(object sender, EventArgs e)
        {
            if (sender == this.button7)  // ゲーム開始ボタン
            {
                boardclass.InitBoard(COLOR_BLACK);
                m_hintList.Clear();
                cppWrapper.ReleaseHash();
                nowColor = boardclass.GetNowColor();
                SetPlayerInfo();

                m_mode = ON_GAME;
                m_hint_mode = ON_NOTHING;
                ChangePlayer();


                if (m_game_mode == BATTLE_MODE)
                {
                    if (nowPlayer.playerInfo == Player.PLAYER_CPU)
                    {
                        // CPUモードに移行(ハンドラコール)
                        m_cpuFlagProperty = true;
                    }
                    else
                    {
                        panel1.Invalidate(false);
                        panel1.Update();
                    }
                }
                else
                {
                    comboBox1.SelectedIndex = 0;
                    comboBox2.SelectedIndex = 0;
                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                    m_hintLevel = 6;
                    m_hintList.Clear();
                    m_mode = ON_GAME;
                    m_game_mode = ANALYZE_MODE;
                    m_hint_mode = ON_HINT;
                    m_hintEvalMax = -INFINITY_SCORE;
                    // ヒント処理ハンドラをコール
                    m_hintFlagProperty = true;
                }


                panel1.Invalidate(false);
                panel1.Update();
            }
            else if (sender == button5 && m_mode == ON_NOTHING) // ゲーム再開ボタン
            {
                m_mode = ON_GAME;

                toolStripStatusLabel1.Text = "";
                toolStripStatusLabel2.Text = "";
                toolStripStatusLabel3.Text = "";
                //toolStripStatusLabel4.Text = "";

                cppWrapper.ReleaseHash();
                m_hintList.Clear();
                boardclass.DeleteHistory(boardclass.GetNowTurn());
                nowColor = boardclass.GetNowColor();

                SetPlayerInfo();
                ChangePlayer();

                if(m_game_mode == BATTLE_MODE)
                {
                    if (nowPlayer.playerInfo == Player.PLAYER_CPU)
                    {
                        // CPUモードに移行(ハンドラコール)
                        m_cpuFlagProperty = true;
                    }
                    else
                    {
                        panel1.Invalidate(false);
                        panel1.Update();
                    }
                }
                else
                {
                    comboBox1.SelectedIndex = 0;
                    comboBox2.SelectedIndex = 0;
                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                    m_hintLevel = 6;
                    m_hintList.Clear();
                    m_mode = ON_GAME;
                    m_game_mode = ANALYZE_MODE;
                    m_hint_mode = ON_HINT;
                    m_hintEvalMax = -INFINITY_SCORE;
                    // ヒント処理ハンドラをコール
                    m_hintFlagProperty = true;
                }
                
            }
            else if (sender == this.button6) // 中断ボタン
            {
                if (m_cpuFlagProperty == false)
                {
                    hintAbort();
                    if (m_hint_mode == ON_HINT)
                    {
                        //MessageBox.Show(
                        //"ヒントの処理を中断しました。",
                        //"中断",
                        //MessageBoxButtons.OK,
                        //MessageBoxIcon.Information);

                        toolStripStatusLabel1.Text = "";
                        toolStripStatusLabel2.Text = "";
                        toolStripStatusLabel3.Text = "Hint aborted.";
                        label6.Text = "";
                    }
                    if(m_game_mode == ANALYZE_MODE) m_hint_mode = ON_HINT_FINISH;
                    else m_hint_mode = ON_NOTHING;

                    m_mode = ON_NOTHING;
                    this.Cursor = Cursors.Default;
                    SetControlEnable(true);
                    panel1.Invalidate(false);
                    panel1.Update();
                }
                else
                {
                    // CPUスレッドに停止命令送信
                    m_abort = true;
                    m_mode = ON_NOTHING;
                    cppWrapper.SendAbort();
                    this.Cursor = Cursors.Default;
                    MessageBox.Show(
                        "AIの処理を中断しました。",
                        "中断",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            else if (sender == this.button1) // 最初に戻るボタン
            {
                bool ret = boardclass.SetHistory(0);
                if (ret == true)
                {
                    m_mode = ON_NOTHING;
                    ChangePlayer();
                    panel1.Invalidate(false);
                    panel1.Update();
                }
            }
            else if (sender == this.button2) // 最新に進むボタン
            {
                bool ret = boardclass.SetHistory(boardclass.GetRecentTurn());
                if (ret == true)
                {
                    m_mode = ON_NOTHING;
                    ChangePlayer();
                    panel1.Invalidate(false);
                    panel1.Update();
                }
            }
            else if (sender == this.button3) // 一手戻るボタン
            {
                bool ret = boardclass.SetHistory(boardclass.GetNowTurn() - 1);
                if (ret == true)
                {
                    m_mode = ON_NOTHING;
                    ChangePlayer();
                    panel1.Invalidate(false);
                    panel1.Update();
                }
            }
            else if (sender == this.button4) // 一手進むボタン
            {
                bool ret = boardclass.SetHistory(boardclass.GetNowTurn() + 1);
                if (ret == true)
                {
                    m_mode = ON_NOTHING;
                    ChangePlayer();
                    panel1.Invalidate(false);
                    panel1.Update();
                }
            }
        }

        private void hintAbort()
        {
            if (hintGmt != null)
            {
                hintGmt.AbortAll();
            }
        }

        private void ChangePlayer()
        {
            nowColor = boardclass.GetNowColor();
            nowPlayer = playerArray[nowColor];
            if (nowColor == COLOR_BLACK)
            {
                label1.BackColor = Color.PowderBlue;
                label2.BackColor = Control.DefaultBackColor;
            }
            else
            {
                label2.BackColor = Color.PowderBlue;
                label1.BackColor = Control.DefaultBackColor;
            }
        }

        private void SetPlayerInfo()
        {
            if (comboBox1.SelectedIndex == 0)
            {
                playerArray[0] = new Player(Player.PLAYER_HUMAN, COLOR_BLACK);
            }
            else
            {
                playerArray[0] = new Player(Player.PLAYER_CPU, COLOR_BLACK);
            }
            if (comboBox2.SelectedIndex == 0)
            {
                playerArray[1] = new Player(Player.PLAYER_HUMAN, COLOR_WHITE);
            }
            else
            {
                playerArray[1] = new Player(Player.PLAYER_CPU, COLOR_WHITE);
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bkImg = new Bitmap(KZreversi.Properties.Resources.othello_bk);
            whImg = new Bitmap(KZreversi.Properties.Resources.othello_wh);
            m_back_bitmap = new Bitmap("src\\wood_pattern.png"); 
            m_panel1_bitmap = new Bitmap("src\\othello_board.png"); 

            // 盤面背景のセット
            panel_back = new BufferedPanel(false);
            panel_back.Width = 534;
            panel_back.Height = 534;
            panel_back.Location = new Point(0, 0);
            panel_back.Paint += panel_back_Paint;
            panel_back.BackgroundImageLayout = ImageLayout.Stretch;
            panel_back.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this.Controls.Add(panel_back);

            // 盤面のセット
            panel_board = new BufferedPanel(false);
            panel_board.Width = 480;
            panel_board.Height = 480;
            panel_board.Location = new Point(26, 30);
            panel_board.BackColor = Color.Transparent;
            panel_board.Paint += panel_board_Paint;
            panel_board.Resize += panel_Resize;
            panel_board.BackgroundImageLayout = ImageLayout.Zoom;
            panel_board.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            // 盤面背景の子として登録
            panel_back.Controls.Add(panel_board);

            // 盤面のセット
            panel1 = new BufferedPanel(true);
            panel1.Width = 480;
            panel1.Height = 480;
            panel1.Location = new Point(0, 0);
            panel1.BackColor = Color.Transparent;
            panel1.Paint += panel1_Paint;
            panel1.Click += panel1_Click;
            panel1.DoubleClick += panel1_Click;
            panel1.Resize += panel_Resize;
            panel1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            // 盤面背景の子として登録
            panel_board.Controls.Add(panel1);

            m_board_width = panel1.Width - (panel1.Width * border_rate);
            m_board_height = panel1.Height - (panel1.Height * border_rate);

            // 画像とフォントのスケーリング
            resize_stone(panel1);

            LoadForm lf = new LoadForm();
            lf.ShowDialog(this);

            if (loadResult == false)
            {
                MessageBox.Show(
                    "DLLかAIデータ読み込みに失敗しました。アプリケーションを終了します。",
                    "読み込みエラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                this.Close();
            }

            // !! ステータスバーのみC#のメモリ管理下から外すため自力解放必須 !!
            // このdelegateをGC対象外にする
            m_gcHandle_setCpuMessageDelegate = GCHandle.Alloc(cpuMessageDelegate);
            // C++側から呼び出せるようにする
            cpuMessageDelegatePtr = Marshal.GetFunctionPointerForDelegate(cpuMessageDelegate);
            // C側に関数ポインタを登録
            cppWrapper.EntryFunction(cpuMessageDelegatePtr);
            m_gcHandle_setCpuMessageDelegate.Free();

            // CPU情報表示用
            m_gcHandle_setPVLineDelegate = GCHandle.Alloc(setPVLineDelegate);
            setPVLineDelegatePtr = Marshal.GetFunctionPointerForDelegate(setPVLineDelegate);
            cppWrapper.EntryFunction(setPVLineDelegatePtr);
            m_gcHandle_setPVLineDelegate.Free();

            // MPC進捗状況表示用
            m_gcHandle_setMPCInfoDelegate = GCHandle.Alloc(setMPCInfoDelegate);
            setMPCInfoDelegatePtr = Marshal.GetFunctionPointerForDelegate(setMPCInfoDelegate);
            cppWrapper.EntryFunction(setMPCInfoDelegatePtr);
            m_gcHandle_setMPCInfoDelegate.Free();

            // デフォルトのCPU設定
            for (int i = 0; i < cpuClass.Length; i++)
            {
                if (i == 0)
                {
                    cpuClass[i].SetColor(BoardClass.BLACK);
                }
                else
                {
                    cpuClass[i].SetColor(BoardClass.WHITE);
                }

                // size(MB)--> size * 1024 * 1024 / sizeof(hash_entry) = size * 1024 * 16
                cpuClass[i].SetCasheSize(32 * 1024 * 16); // 32MB default
                cpuClass[i].SetSearchDepth(6);
                cpuClass[i].SetWinLossDepth(14);
                cpuClass[i].SetExactDepth(12);
                cpuClass[i].SetBookFlag(1);
                cpuClass[i].SetBookVariability(1);
                cpuClass[i].SetMpcFlag(Convert.ToByte(MenuUseMPC.Checked));
                cpuClass[i].SetTableFlag(Convert.ToByte(MenuUseTable.Checked));
            }

            m_enablePVdisplay = MenuDisplayPV.Checked;
            m_enableDetailDisplay = MenuAIThinking.Checked;
            m_enableResultDisplay = MenuResultDetail.Checked;
        }


        void resize_stone(BufferedPanel panel)
        {
            //label5.Text = "x=" + panel.Width + "y=" + panel.Height;
            float board_x, board_y;
            // 縦横の小さい方に合わせる
            if (panel.Width < panel.Height)
            {
                // margin考慮
                board_x = panel.Width - (panel.Width * border_rate);
                board_y = panel.Height - (panel.Width * border_rate);
                m_scale = board_x / (m_board_width + 16);
                m_mass_size = board_x / BOARD_SIZE;
                m_fix_x = (float)30.5 * m_scale;
                m_fix_y = (board_y - board_x) / 2 + ((float)30.5 * m_scale);
            }
            else
            {
                board_x = panel.Width - (panel.Height * border_rate);
                board_y = panel.Height - (panel.Height * border_rate);
                m_scale = board_y / (m_board_height + 16);
                m_mass_size = board_y / BOARD_SIZE;
                m_fix_x = (board_x - board_y) / 2 + ((float)30.5 * m_scale);
                m_fix_y = (float)30.5 * m_scale;
            }

            if (m_scale != 0)
            {
                // 各描画インスタンスの座標をスケーリング
                stone_size_x = (m_board_width / BOARD_SIZE) * m_scale;
                stone_size_y = (m_board_height / BOARD_SIZE) * m_scale;
                last_move_fix_x = (21 * m_scale) + m_fix_x;
                last_move_fix_y = (21 * m_scale) + m_fix_y;
                canmove_x = (19 * m_scale) + m_fix_x;
                canmove_y = (21 * m_scale) + m_fix_y;
                font_scale_x = (2 * m_scale) + m_fix_x;
                font_scale_y = (14 * m_scale) + m_fix_y;

                // フォントスケーリング
                m_ft = new Font("MS UI Gothic", 9 * m_scale, FontStyle.Bold);
                m_ft2 = new Font("MS UI Gothic", 8 * m_scale);
                m_ft3 = new Font("Arial", 18 * m_scale, FontStyle.Bold | FontStyle.Italic);
                m_ft4 = new Font("Arial", 15 * m_scale, FontStyle.Bold | FontStyle.Italic);
            }
        }


        void resize_board(BufferedPanel bp) 
        {
            if (bp.Width < bp.Height)
            {
                m_board_start_x = 0;
                m_board_start_y = (bp.Height - bp.Width) / 2;
            }
            else
            {
                m_board_start_x = (bp.Width - bp.Height) / 2;
                m_board_start_y = 0;
            }
        }

        // 盤面のリサイズ処理
        void panel_Resize(object sender, EventArgs e)
        {
            BufferedPanel pl = (BufferedPanel)sender;
            if(pl == panel1)
            {
                // 石やフォントのスケーリング処理
                resize_stone(pl);
            }
            else if(pl == panel_board)
            {
                // 盤面のスケーリング
                resize_board(pl);
            }
        }

        private float stone_size_x, stone_size_y;
        private float last_move_fix_x, last_move_fix_y;
        private float canmove_x, canmove_y;
        private float font_scale_x, font_scale_y;
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            int pos;
            ulong temp;
            BufferedPanel bp = (BufferedPanel)sender;

            //listBox1.Items[0] = m_mode.ToString();
            //listBox1.Items[1] = m_hint_mode.ToString();

            // 盤面の石などの描画
            nowPlayer = playerArray[nowColor];
            // 盤面情報から描画
            temp = boardclass.GetBlack();
            while (temp > 0)
            {
                pos = cppWrapper.ConvertMoveBit(temp);
                e.Graphics.DrawImage(bkImg,
                    (pos / BOARD_SIZE) * m_mass_size + m_fix_x,
                    (pos % BOARD_SIZE) * m_mass_size + m_fix_y,
                    stone_size_x, stone_size_y);
                temp ^= (1UL << pos);
            }

            temp = boardclass.GetWhite();
            while (temp > 0)
            {
                pos = cppWrapper.ConvertMoveBit(temp);
                e.Graphics.DrawImage(whImg,
                    (pos / BOARD_SIZE) * m_mass_size + m_fix_x,
                    (pos % BOARD_SIZE) * m_mass_size + m_fix_y,
                    stone_size_x, stone_size_y);
                temp ^= (1UL << pos);
            }

            // 最後に打った手を強調する
            pos = boardclass.GetRecentMove();
            if (pos >= 0)
            {
                e.Graphics.DrawString("●", m_ft2, Brushes.OrangeRed,
                (pos / BOARD_SIZE) * m_mass_size + last_move_fix_x,
                (pos % BOARD_SIZE) * m_mass_size + last_move_fix_y);
            }

            // ゲーム中の場合かつプレイヤーの手番の場合、着手可能場所を表示
            if (m_mode == ON_GAME && m_hint_mode == ON_NOTHING && m_cpuFlagProperty == false)
            {
                temp = cppWrapper.GetEnumMove(boardclass);
                nowPlayer.moves = temp;
                if (temp != 0)
                {
                    m_passCount = 0;
                    while (temp > 0)
                    {
                        pos = cppWrapper.ConvertMoveBit(temp);
                        e.Graphics.DrawString("×", m_ft, Brushes.DarkRed,
                            (pos / BOARD_SIZE) * m_mass_size + canmove_x,
                            (pos % BOARD_SIZE) * m_mass_size + canmove_y);
                        temp ^= (1UL << pos);
                    }
                }
            }
            else if ((m_hint_mode == ON_HINT || m_hint_mode == ON_HINT_FINISH) && m_hintList.Count > 0 && m_cpuFlagProperty == false)
            {
                // 記憶したヒントを表示
                int attr, eval;
                bool first;

                temp = cppWrapper.GetEnumMove(boardclass);
                nowPlayer.moves = temp;

                if (m_hintList.Count > 1) first = true; // 探索後の表示のため
                else first = false;

                foreach (var data in m_hintList)
                {
                    attr = data[0];
                    pos = data[1];
                    eval = data[2];

                    if (attr == HintClass.SOLVE_MIDDLE)
                    {
                        dispMiddleEval(e, pos, eval, first);
                    }
                    else if (attr == HintClass.SOLVE_WLD)
                    {
                        dispWldEval(e, pos, eval, first);
                    }
                    else
                    {
                        dispExactEval(e, pos, eval, first);
                    }
                    first = false;
                }
            }
            textBox1.Text = String.Format("0x{0:x}, 0x{1:x}",
                       boardclass.GetBlack(), boardclass.GetWhite());
        }


        private void panel_back_Paint(object sender, PaintEventArgs e)
        {
            
            BufferedPanel bp = (BufferedPanel)sender;
            Rectangle board_rect1;

            if (panel_board.Width < panel_board.Height)
            {
                board_rect1 = new Rectangle((int)(panel_board.Location.X + m_board_start_x),
                    (int)(panel_board.Location.Y + m_board_start_y),
                    panel_board.Width, panel_board.Width);
            }
            else 
            {
                board_rect1 = new Rectangle((int)(panel_board.Location.X + m_board_start_x),
                    (int)(panel_board.Location.Y + m_board_start_y),
                    panel_board.Height, panel_board.Height);
            }

            Rectangle board_rect2 = new Rectangle(bp.Location,
                new Size(bp.Width, bp.Height));

            Region rgn = new Region(board_rect2);
            rgn.Xor(board_rect1);

            e.Graphics.Clip = rgn;
            e.Graphics.DrawImage(m_back_bitmap, 0, 0, bp.Width, bp.Height);

        }


        private void panel_board_Paint(object sender, PaintEventArgs e)
        {
            BufferedPanel bp = (BufferedPanel)sender;

            if (bp.Width < bp.Height)
            {
                e.Graphics.DrawImage(m_panel1_bitmap, m_board_start_x, m_board_start_y, bp.Width, bp.Width);
            }
            else
            {
                e.Graphics.DrawImage(m_panel1_bitmap, m_board_start_x, m_board_start_y, bp.Height, bp.Height);
            }

        }



        private void dispExactEval(PaintEventArgs e, int pos, int eval, bool first)
        {
            string sign;
            float font_fix_x;
            Brush brs;

            if (eval >= 0) // +0 ～ +64
            {
                sign = "+";
                font_fix_x = 3;
                if (eval >= 10) // +10 ～ +64
                {
                    font_fix_x = 0;
                }
            }
            else
            {
                if (eval > -1) // -0
                {
                    sign = "-";
                    font_fix_x = 5;
                }
                else if (eval <= -10) // -10 ～ -64
                {
                    sign = "";
                    font_fix_x = 2;
                }
                else // -1 ～ -9
                {
                    sign = "";
                    font_fix_x = 5;
                }
            }

            font_fix_x *= m_scale;
            // ループの初回が最善手なので目立つよう表示
            if (first || eval >= m_hintEvalMax)
            {
                m_hintEvalMax = eval;
                brs = Brushes.LightGreen;
            }
            else brs = Brushes.DimGray;

            e.Graphics.DrawString(sign + eval, m_ft3, brs,
               (pos / BOARD_SIZE) * m_mass_size + font_scale_x + font_fix_x,
               (pos % BOARD_SIZE) * m_mass_size + font_scale_y);
        }

        private void dispWldEval(PaintEventArgs e, int pos, int eval, bool first)
        {
            string wld;
            float font_fix_x;
            Brush brs;

            if (eval > 0) // +0 ～ +1
            {
                wld = "WIN";
                font_fix_x = 0;
            }
            else if (eval < 0)
            {
                wld = "LOS";
                font_fix_x = 0;
            }
            else
            {
                wld = "DRW";
                font_fix_x = -5;
            }

            font_fix_x *= m_scale;
            // ループの初回が最善手なので目立つよう表示
            if (first == true || eval >= m_hintEvalMax)
            {
                m_hintEvalMax = eval;
                brs = Brushes.Maroon;
            }
            else brs = Brushes.DarkGreen;

            e.Graphics.DrawString(wld, m_ft4, brs,
               (pos / BOARD_SIZE) * m_mass_size + font_scale_x + font_fix_x,
               (pos % BOARD_SIZE) * m_mass_size + font_scale_y);
        }

        private int getCurrentMaxEval()
        {
            int max = -1280000;
            foreach(int[] list in m_hintList)
            {
                if(list[2] > max) max = list[2];
            }

            return max;
        }

        private void dispMiddleEval(PaintEventArgs e, int pos, int eval, bool first)
        {
            string sign;
            float font_fix_x;
            Brush brs;

            // ループの初回が最善手なので目立つよう表示(LightGreen)
            if (eval >= getCurrentMaxEval())
            {
                m_hintEvalMax = eval;
                brs = Brushes.DarkMagenta;
            }
            else
            {
                brs = Brushes.Navy;
            }
            if (eval >= 0) // +0 ～ +64
            {
                if (eval >= 0.0)
                {
                    sign = "+";
                    font_fix_x = 3;
                    if (eval >= 10) // +10 ～ +64
                    {
                        font_fix_x = 0;
                    }
                }
                else
                {
                    sign = "-";
                    font_fix_x = 5;
                }
            }
            else
            {
                if (eval <= -10) // -10 ～ -64
                {
                    sign = "";
                    font_fix_x = 2;
                }
                else // -1 ～ -9
                {
                    sign = "";
                    font_fix_x = 5;
                }
            }

            font_fix_x *= m_scale;

            // 該当のマスに描画
            e.Graphics.DrawString(sign + eval, m_ft3, brs,
               (pos / BOARD_SIZE) * m_mass_size + font_scale_x + font_fix_x,
               (pos % BOARD_SIZE) * m_mass_size + font_scale_y);
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            Point pos;
            int num;
            MouseEventArgs mouseEvent = (MouseEventArgs)e;
            MouseButtons buttons = mouseEvent.Button;

            switch (m_mode)
            {
                case ON_GAME:

                    if (m_cpuFlagProperty == true)
                    {
                        // CPU処理中のため操作無効
                        break;
                    }

                    // 押された瞬間の座標を取得
                    pos = mouseEvent.Location;
                    num = (int)((pos.X - m_fix_x) / m_mass_size) * BOARD_SIZE;
                    num += (int)((pos.Y - m_fix_y) / m_mass_size);
                    // 着手出来るかチェック
                    if ((nowPlayer.moves & (1UL << num)) != 0)
                    {
                        // 着手できたのでいったんヒントを中断して終了するまで待機
                        if(m_game_mode == ANALYZE_MODE && m_hint_mode == ON_HINT)
                        {
                            interruptHint();
                            g_num = num;
                            Task task = Task.Run(() => {
                                while (g_num != -1) ;

                                // 着手に合わせて盤面情報を更新
                                boardclass.move(num);
                                // プレイヤー変更
                                ChangePlayer();

                                // 画面再描画
                                panel1.Invalidate(false);
                                panel1.Update();

                                if (nowPlayer.playerInfo == Player.PLAYER_CPU)
                                {
                                    // CPUモードに移行(ハンドラコール)
                                    m_cpuFlagProperty = true;
                                    toolStripStatusLabel1.Text = "";
                                    toolStripStatusLabel2.Text = "";
                                    toolStripStatusLabel3.Text = "";
                                    SetlabelText("");
                                    return;
                                }

                                // 相手が打てない
                                if (cppWrapper.GetEnumMove(boardclass) == 0)
                                {
                                    m_passCount++;

                                    if (m_passCount == 2)
                                    {
                                        // ゲーム終了
                                        m_mode = ON_NOTHING;
                                        m_cpuFlagProperty = false;
                                        m_passCount = 0;
                                        // 結果表示
                                        PrintResult();
                                        return;
                                    }

                                    MessageBox.Show("プレイヤー" + (nowColor + 1) + "はパスです", "情報",
                                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    // プレイヤー変更
                                    ChangePlayerReasonPass();

                                    if (cppWrapper.GetEnumMove(boardclass) == 0)
                                    {
                                        // 双方が人間でお互いがパスだった場合にここに来る
                                        m_mode = ON_NOTHING;
                                        m_cpuFlagProperty = false;
                                        m_passCount = 0;
                                        // 結果表示
                                        PrintResult();
                                    }
                                }
                                else
                                {
                                    if (m_game_mode == ANALYZE_MODE)
                                    {
                                        resumeHint();
                                    }
                                }
                            });
                        }
                        else
                        {
                            // 着手に合わせて盤面情報を更新
                            boardclass.move(num);
                            // プレイヤー変更
                            ChangePlayer();

                            // 画面再描画
                            panel1.Invalidate(false);
                            panel1.Update();

                            if (nowPlayer.playerInfo == Player.PLAYER_CPU)
                            {
                                // CPUモードに移行(ハンドラコール)
                                m_cpuFlagProperty = true;
                                toolStripStatusLabel1.Text = "";
                                toolStripStatusLabel2.Text = "";
                                toolStripStatusLabel3.Text = "";
                                SetlabelText("");
                                return;
                            }

                            // 相手が打てない
                            if (cppWrapper.GetEnumMove(boardclass) == 0)
                            {
                                m_passCount++;

                                if (m_passCount == 2)
                                {
                                    // ゲーム終了
                                    m_mode = ON_NOTHING;
                                    m_cpuFlagProperty = false;
                                    m_passCount = 0;
                                    // 結果表示
                                    PrintResult();
                                    return;
                                }

                                MessageBox.Show("プレイヤー" + (nowColor + 1) + "はパスです", "情報",
                                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                                // プレイヤー変更
                                ChangePlayerReasonPass();

                                if (cppWrapper.GetEnumMove(boardclass) == 0)
                                {
                                    // 双方が人間でお互いがパスだった場合にここに来る
                                    m_mode = ON_NOTHING;
                                    m_cpuFlagProperty = false;
                                    m_passCount = 0;
                                    // 結果表示
                                    PrintResult();
                                }
                            }
                            else
                            {
                                if (m_game_mode == ANALYZE_MODE)
                                {
                                    resumeHint();
                                }
                            }
                        }
                    }

                    break;
                case ON_EDIT:
                    // エディットモードの処理

                    // 押された瞬間の座標を取得
                    pos = mouseEvent.Location;
                    num = (int)((pos.X - m_fix_x) / m_mass_size) * BOARD_SIZE;
                    num += (int)((pos.Y - m_fix_y) / m_mass_size);
                    ulong posBit = (1UL << num);
                    ulong bk = boardclass.GetBlack();
                    ulong wh = boardclass.GetWhite();

                    if (buttons == MouseButtons.Left)
                    {
                        // 黒を置くor白を置く
                        if ((bk & posBit) != 0)
                        {
                            // 黒がすでに置いてある場合は白にする
                            boardclass.EditBoard(bk & ~posBit, wh | posBit);
                        }
                        else if ((wh & posBit) != 0)
                        {
                            // 白がすでに置いてある場合は黒にする
                            boardclass.EditBoard(bk | posBit, wh & ~posBit);
                        }
                        else
                        {
                            // 置いてない場合は黒を置く
                            boardclass.EditBoard(bk | posBit, wh);
                        }

                    }
                    else
                    {
                        // 消す
                        boardclass.EditBoard(bk & ~posBit, wh & ~posBit);
                    }

                    textBox1.Text = String.Format("bk = 0x{0:x}; wh = 0x{1:x};", 
                        boardclass.GetBlack(), boardclass.GetWhite());
                    textBox1.Refresh();


                    // 画面再描画
                    panel1.Invalidate(false);
                    panel1.Update();

                    break;
                default:
                    // 何もしない
                    break;
            }
        }

        private void interruptHint()
        {
            if (m_hint_mode == ON_HINT)
            {
                hintAbort();
            }
            m_hint_mode = ON_NOTHING;
            if (m_hintList != null) m_hintList.Clear();
        }

        private void resumeHint()
        {
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox1.Enabled = false;
            comboBox2.Enabled = false;
            m_hintLevel = 6;
            m_hintList.Clear();
            m_hint_mode = ON_HINT;
            m_hintEvalMax = -INFINITY_SCORE;
            // ヒント処理ハンドラをコール
            m_hintFlagProperty = true;
        }

        

        private void ChangePlayerReasonPass()
        {
            boardclass.ChangeColor();
            ChangePlayer();
        }

        private void setMove(ulong move)
        {
            m_cpuMoveProperty = move;
        }

        private void setNodeCount(ulong nodeCount)
        {
            StringBuilder timerSb = new StringBuilder(256);
            string temp;

            // 探索済みノード数
            timerSb.Append("node:");

            if (nodeCount >= 1000000000)  // Gn
            {
                timerSb.Append((nodeCount / (double)1000000000).ToString("f2"));
                timerSb.Append("[Gn]");
            }
            else if (nodeCount >= 1000000) // Mn
            {
                timerSb.Append((nodeCount / (double)1000000).ToString("f2"));
                timerSb.Append("[Mn]");
            }
            else if (nodeCount >= 1000) // Kn
            {
                timerSb.Append((nodeCount / (double)1000).ToString("f2"));
                timerSb.Append("[Kn]");
            }
            else
            {
                timerSb.Append(nodeCount);
                timerSb.Append("[n]");
            }

            // 経過時間
            timerSb.Append(" time:");
            timerSb.Append((m_sw.ElapsedMilliseconds / (double)1000).ToString("f2"));

            // NPS(node per second)
            timerSb.Append(" nps:");
            temp = ((nodeCount / (m_sw.ElapsedMilliseconds / (double)1000)) / 1000).ToString("f0");
            timerSb.Append(temp);
            timerSb.Append("[Knps]");

            toolStripStatusLabel1.Text = timerSb.ToString();
        }

        private void SetCpuMessage(string cpuMessage)
        {
            if(m_enableResultDisplay) toolStripStatusLabel3.Text = cpuMessage;
        }

        private void SetlabelText(string text) 
        {
            label6.Text = text;
        }
        private void SetPVLine(string cpuMessage)
        {
            if(m_enablePVdisplay) Invoke(new SetPVLineDelegate(SetlabelText), cpuMessage);
        }

        private void SetMPCInfo(string mpcMessage)
        {
            toolStripStatusLabel2.Text = mpcMessage;
        }

        private ulong _m_cpuMove;
        public ulong m_cpuMoveProperty
        {
            get
            {
                return _m_cpuMove;
            }
            set
            {
                _m_cpuMove = value;
                OnPropertyChanged("CpuMove");
            }
        }

        public bool m_cpuFlagProperty
        {
            get
            {
                return _m_cpuFlag;
            }
            set
            {
                _m_cpuFlag = value;
                if (value == true)
                {
                    OnPropertyChanged("CpuMode");
                }
            }
        }

        public bool m_hintFlagProperty
        {
            get
            {
                return _m_hintFlag;
            }
            set
            {
                _m_hintFlag = value;
                if (value == true)
                {
                    OnPropertyChanged("Hint");
                }
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = null;

            if (name == "CpuMove")
            {
                this.Cursor = Cursors.Default;
                handler = PropertyChangedCpuMove;
            }
            else if (name == "CpuMode")
            {
                this.Cursor = Cursors.WaitCursor;
                handler = PropertyChangedCpuMode;
            }
            else if (name == "Hint")
            {
                this.Cursor = Cursors.WaitCursor;
                handler = PropertyChangedHint;
            }

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public void PropertyChangedCpuMove(object sender, PropertyChangedEventArgs e)
        {
            bool bootRet;
            // CPUの算出した評価値を取得
            int eval = cppWrapper.GetLastEvaluation();
            // 評価値の表示
            toolStripStatusLabel2.Text = ConvertEvaltoString(eval, cpuClass[nowColor]);

            if (m_cpuMoveProperty == MOVE_PASS)
            {
                // CPUはパス
                m_passCount++;

                // ゲーム終了？
                if (m_passCount == 2 || cppWrapper.CountBit(~(boardclass.GetBlack() | boardclass.GetWhite())) == 0)
                {
                    // ゲーム終了
                    m_mode = ON_NOTHING;
                    m_cpuFlagProperty = false;
                    m_passCount = 0;
                    m_sw.Stop();
                    SetControlEnable(true);
                    // 結果表示
                    PrintResult();
                    // 画面描画
                    panel1.Invalidate(false);
                    panel1.Update();

                    return;
                }

                MessageBox.Show("プレイヤー" + (nowColor + 1) + "はパスです", "情報",
                   MessageBoxButtons.OK, MessageBoxIcon.Information);

                // CPUがパスなのでプレイヤー変更
                ChangePlayerReasonPass();
            }
            else
            {
                // CPUが打てたのでパスカウントをリセット
                m_passCount = 0;
                // 盤面情報更新
                bootRet = boardclass.move(cppWrapper.ConvertMoveBit(m_cpuMoveProperty));
                if (bootRet == false)
                {
                    MessageBox.Show("内部エラーが発生しました", "エラー",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // CPUが打ったのでプレイヤー変更
                ChangePlayer();
                SetControlEnable(true);
            }

            GameThread gmt;
            // プレイヤー変更後もCPUなら再度ゲームスレッドにリクエスト送信(双方がCPUの場合)
            if (m_mode == ON_GAME && nowPlayer.playerInfo == Player.PLAYER_CPU)
            {
                // 画面再描画(前のCPUの手を画面に反映しておく)
                panel1.Invalidate(false);
                panel1.Update();

                gmt = GameThread.getInstance();
                object[] args = new object[] { GameThread.CMD_CPU, boardclass, cpuClass[nowColor], this };
                gmt.RecvcmdProperty = args;
                m_sw.Restart();

                return;
            }

            // 人間がパスだった場合は通知して再度ゲームスレッドにリクエスト送信
            if (nowPlayer.playerInfo == Player.PLAYER_HUMAN && cppWrapper.GetEnumMove(boardclass) == 0)
            {
                // 画面再描画(前のCPUの手を画面に反映しておく)
                panel1.Invalidate(false);
                panel1.Update();

                m_sw.Stop();

                m_passCount++;

                if (m_passCount == 2 || cppWrapper.CountBit(~(boardclass.GetBlack() | boardclass.GetWhite())) == 0)
                {
                    // ゲーム終了
                    m_mode = ON_NOTHING;
                    m_cpuFlagProperty = false;
                    m_passCount = 0;
                    SetControlEnable(true);
                    // 結果表示
                    PrintResult();
                    // 画面描画
                    panel1.Invalidate(false);
                    panel1.Update();

                    return;
                }

                MessageBox.Show("プレイヤー" + (nowColor + 1) + "はパスです", "情報",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
                m_sw.Start();

                // プレイヤー変更
                ChangePlayerReasonPass();
                // ゲームスレッドにCPU処理リクエスト送信
                gmt = GameThread.getInstance();
                object[] args = new object[] { GameThread.CMD_CPU, boardclass, cpuClass[nowColor], this };
                gmt.RecvcmdProperty = args;
                m_sw.Restart();
                // 画面再描画
                panel1.Invalidate(false);
                panel1.Update();
                return;

            }

            // ここに来るのは次のプレイヤーが人間でかつ手を打てる状態
            m_cpuFlagProperty = false;
            m_passCount = 0;
            SetControlEnable(true);

            hintAbort();
            //while (m_mode == ON_HINT);
            if(m_hint_mode == ON_HINT_FINISH)
            {
                // ヒントモードなら再度ヒント起動
                m_hintEvalMax = -INFINITY_SCORE;
                m_hintList.Clear();
                // ヒント処理ハンドラをコール
                m_hintFlagProperty = true;
            }

            // 画面再描画
            panel1.Invalidate(false);
            panel1.Update();

        }

        private string ConvertEvaltoString(int eval, CpuClass cpu)
        {
            StringBuilder evalSb = new StringBuilder();

            int empty = cppWrapper.CountBit(~(boardclass.GetBlack() | boardclass.GetWhite()));

            if (empty <= cpu.GetExactDepth())
            {
                if (eval >= 0)
                {
                    evalSb.Append("+");
                }

                evalSb.Append(eval);
            }
            else if (empty <= cpu.GetWinLossDepth())
            {
                if (eval > 0)
                {
                    evalSb.Append("WIN");
                }
                else if (eval < 0)
                {
                    evalSb.Append("LOSS");
                }
                else
                {
                    evalSb.Append("DRAW");
                }
            }
            else
            {
                if (eval >= 0)
                {
                    evalSb.Append("+");
                }

                evalSb.Append((eval / (double)EVAL_THRESHOLD).ToString("f3"));
            }

            // CPUが定石から手を算出した場合
            if (cppWrapper.GetIsUseBook())
            {
                evalSb.Append("(book)");
            }

            // 中断ボタンが押された場合
            if (m_abort)
            {
                evalSb.Append("?(abort)");
                m_cpuFlagProperty = false;
                m_abort = false;
            }

            return evalSb.ToString();

        }

        public void PropertyChangedCpuMode(object sender, PropertyChangedEventArgs e)
        {
            // UIを中断ボタン以外無効化
            SetControlEnable(false);
            // ゲームスレッドにCPU処理リクエスト送信
            GameThread gmt = GameThread.getInstance();
            object[] args = new object[] { GameThread.CMD_CPU, boardclass, cpuClass[nowColor], this };
            //MessageBox.Show("ゲームスレッドにCPU処理リクエスト送信", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            gmt.RecvcmdProperty = args;

            toolStripStatusLabel1.Text = "";
            m_sw.Restart();
        }


        public void PropertyChangedHint(object sender, PropertyChangedEventArgs e)
        {
            // UIを中断ボタン以外無効化
            SetControlEnable(false);
            // ゲームスレッドにヒント処理リクエストを送信
            hintGmt = GameThread.getInstance();
            object[] args = new object[] { GameThread.CMD_HINT, boardclass, cpuClass[nowColor], this, m_hintLevel };
            hintGmt.RecvcmdProperty = args;
        }


        private void PrintResult()
        {
            String msg;
            String winStr;
            ulong bk = boardclass.GetBlack();
            ulong wh = boardclass.GetWhite();
            int bkCnt = cppWrapper.CountBit(bk);
            int whCnt = cppWrapper.CountBit(wh);

            if (bkCnt - whCnt > 0)
            {
                winStr = "黒の勝ち";
            }
            else if (bkCnt - whCnt < 0)
            {
                winStr = "白の勝ち";
            }
            else
            {
                winStr = "引き分け";
            }

            msg = String.Format("黒{0:D}-白{1:D}で{2}です。", bkCnt, whCnt, winStr);
            MessageBox.Show(msg, "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }



        private void doHintProcess(HintClass hintData)
        {
            if (hintData != null)
            {
                if (m_firsthint == true)
                {
                    m_hintList.Clear();
                    m_firsthint = false;
                }
                if (hintData.GetPos() == 64)
                {
                    // 最大評価値を初期化
                    m_hintEvalMax = -INFINITY_SCORE;
                }
                else
                {
                    // ヒントデータ更新
                    int attr = hintData.GetAttr();
                    int position = hintData.GetPos();
                    int index = findIndexFromPosition(position);
                    if (index == -1)
                    {
                        // 新規データ
                        m_hintList.Add(new int[] { attr, position, hintData.GetEval() });
                    }
                    else
                    {
                        // 更新
                        m_hintList[index] = new int[] { attr, position, hintData.GetEval() };
                    }
                    // ソート処理
                    m_hintList.Sort(CompareEval);
                }

                // 画面再描画
                panel1.Invalidate(false);
                panel1.Update();
            }
            else
            {
                // 終了通知
                m_hintEvalMax = -INFINITY_SCORE;
                m_hintFlagProperty = false;
                SetControlEnable(true);
                this.Cursor = Cursors.Default;
                toolStripStatusLabel1.Text = "";
                toolStripStatusLabel2.Text = "";
                toolStripStatusLabel3.Text = "Hint finished.";
                label6.Text = "";
                m_hint_mode = ON_HINT_FINISH;
                m_firsthint = true;
                g_num = -1;
            }
        }

        private int CompareEval(int[] x, int[] y)
        {
            return y[2] - x[2];
        }

        int findIndexFromPosition(int pos)
        {
            int i = 0, index = -1;
            foreach (var data in m_hintList)
            {
                if (data[1] == pos)
                {
                    index = i;
                    break;
                }
                i++;
            }

            return index;
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cbox = (ComboBox)sender;
            int index = cbox.SelectedIndex;
            uint color;

            if (index != 0 && index < 13)
            {
                if (cbox == this.comboBox1)
                {
                    cpuClass[BoardClass.BLACK].SetColor(BoardClass.BLACK);
                    color = BoardClass.BLACK;
                }
                else
                {
                    cpuClass[BoardClass.WHITE].SetColor(BoardClass.WHITE);
                    color = BoardClass.WHITE;
                }

                cpuClass[color].SetSearchDepth((uint)index * 2);
                cpuClass[color].SetWinLossDepth(dcTable[index - 1]);
                cpuClass[color].SetExactDepth(dcTable[index - 1] - 2);

                if (playerArray != null)
                {
                    if (cbox == comboBox1) playerArray[0].playerInfo = Player.PLAYER_CPU;
                    else playerArray[1].playerInfo = Player.PLAYER_CPU;
                }
                    
            }
            else if (index == 13)
            {
                // 強制勝敗探索モード
                if (cbox == this.comboBox1)
                {
                    cpuClass[BoardClass.BLACK].SetColor(BoardClass.BLACK);
                    color = BoardClass.BLACK;
                }
                else
                {
                    cpuClass[BoardClass.WHITE].SetColor(BoardClass.WHITE);
                    color = BoardClass.WHITE;
                }

                cpuClass[color].SetSearchDepth(0);
                cpuClass[color].SetWinLossDepth(60);
                cpuClass[color].SetExactDepth(0);

                if (playerArray != null)
                {
                    if (cbox == comboBox1) playerArray[0].playerInfo = Player.PLAYER_CPU;
                    else playerArray[1].playerInfo = Player.PLAYER_CPU;
                }
            }
            else if (index == 14)
            {
                // 強制石差探索モード
                if (cbox == this.comboBox1)
                {
                    cpuClass[BoardClass.BLACK].SetColor(BoardClass.BLACK);
                    color = BoardClass.BLACK;
                }
                else
                {
                    cpuClass[BoardClass.WHITE].SetColor(BoardClass.WHITE);
                    color = BoardClass.WHITE;
                }

                cpuClass[color].SetSearchDepth(0);
                cpuClass[color].SetWinLossDepth(0);
                cpuClass[color].SetExactDepth(60);

                if (playerArray != null)
                {
                    if (cbox == comboBox1) playerArray[0].playerInfo = Player.PLAYER_CPU;
                    else playerArray[1].playerInfo = Player.PLAYER_CPU;
                }
            }
            else if(playerArray != null)
            {
                if(cbox == comboBox1) playerArray[0].playerInfo = Player.PLAYER_HUMAN;
                else  playerArray[1].playerInfo = Player.PLAYER_HUMAN;
            }

            if (index != 0)
            {
                if(m_game_mode == ANALYZE_MODE)
                {
                    interruptHint();
                    MessageBox.Show("CPUが選択されたため、解析モードを終了します。", "解析モード終了",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    m_mode = ON_NOTHING;
                    m_game_mode = BATTLE_MODE;

                    hintToolStripMenuItem.Text = "解析モード";

                    panel1.Invalidate(false);
                    panel1.Update();
                }
            }
        }

        private void 新規ゲームToolStripMenuItem_Click(object sender, EventArgs e)
        {
            boardclass.InitBoard(COLOR_BLACK);
            cppWrapper.ReleaseHash();
            nowColor = boardclass.GetNowColor();
            SetPlayerInfo();

            m_mode = ON_GAME;

            panel1.Invalidate(false);
            panel1.Update();
        }

        private void 盤面編集ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_mode = ON_EDIT;
        }

        private void panel1_DoubleClick(object sender, EventArgs e)
        {
            Point pos;
            int num;
            MouseEventArgs mouseEvent = (MouseEventArgs)e;
            MouseButtons buttons = mouseEvent.Button;

            switch (m_mode)
            {
                case ON_EDIT:
                    // エディットモードの処理

                    // 押された瞬間の座標を取得
                    pos = mouseEvent.Location;
                    num = ((pos.X / 60) * BOARD_SIZE) + (pos.Y / 60);
                    ulong posBit = (1UL << num);
                    ulong bk = boardclass.GetBlack();
                    ulong wh = boardclass.GetWhite();

                    if (buttons == MouseButtons.Left)
                    {
                        // 黒を置くor白を置く
                        if ((bk & posBit) != 0)
                        {
                            // 黒がすでに置いてある場合は白にする
                            boardclass.EditBoard(bk & ~posBit, wh | posBit);
                        }
                        else if ((wh & posBit) != 0)
                        {
                            // 白がすでに置いてある場合は黒にする
                            boardclass.EditBoard(bk | posBit, wh & ~posBit);
                        }
                        else
                        {
                            // 置いてない場合は黒を置く
                            boardclass.EditBoard(bk | posBit, wh);
                        }

                    }
                    else
                    {
                        // 消す
                        boardclass.EditBoard(bk & ~posBit, wh & ~posBit);
                    }

                    textBox1.Text = String.Format("0x{0:x}, 0x{1:x}",
                        boardclass.GetBlack(), boardclass.GetWhite());
                    textBox1.Refresh();

                    // 画面再描画
                    panel1.Invalidate(false);
                    panel1.Update();

                    break;
                default:
                    // 何もしない
                    break;
            }
        }

        private void 盤面初期化ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            boardclass.InitBoard(COLOR_BLACK);
            nowColor = boardclass.GetNowColor();
            SetPlayerInfo();
            panel1.Invalidate(false);
            panel1.Update();
        }

        private void mPC探索を行うToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (MenuUseMPC.Checked)
            {
                cpuClass[0].SetMpcFlag(0);
                cpuClass[1].SetMpcFlag(0);
                MenuUseMPC.Checked = false;
            }
            else
            {
                cpuClass[0].SetMpcFlag(1);
                cpuClass[1].SetMpcFlag(1);
                MenuUseMPC.Checked = true;
            }
        }


        private void 置換表を使うToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MenuUseTable.Checked)
            {
                cpuClass[0].SetTableFlag(0);
                cpuClass[1].SetTableFlag(0);
                MenuUseTable.Checked = false;
            }
            else
            {
                cpuClass[0].SetTableFlag(1);
                cpuClass[1].SetTableFlag(1);
                MenuUseTable.Checked = true;
            }
        }

        private void bookを使用ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (BOOKFLAG_ToolStripMenuItem.Checked)
            {
                cpuClass[0].SetBookFlag(0);
                cpuClass[1].SetBookFlag(0);
                BOOKFLAG_ToolStripMenuItem.Checked = false;
            }
            else
            {
                cpuClass[0].SetBookFlag(1);
                cpuClass[1].SetBookFlag(1);
                BOOKFLAG_ToolStripMenuItem.Checked = true;
            }
        }

        private void FFOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            uint color;
            ulong bk, wh;

            m_mode = ON_NOTHING;

            if (sender == toolStripFFO40) 
            {
                //FFO40
                bk = 9158069842325798912;
                wh = 11047339776155165;
                color = COLOR_BLACK;
            }
            else if (sender == toolStripFFO41)
            {
                //FFO41
                bk = 616174399789064;
                wh = 39493460025648416;
                color = COLOR_BLACK;
            }
            else if (sender == toolStripFFO42)
            {
                //FFO42
                bk = 22586176447709200;
                wh = 9091853944868375556;
                color = COLOR_BLACK;
            }
            else if (sender == toolStripFFO43)
            {
                //FFO43
                bk = 38808086923902976;
                wh = 13546258740034592;
                color = COLOR_WHITE;
            }
            else if (sender == toolStripFFO44)
            {
                //FFO44
                bk = 2494790880993312;
                wh = 1010251075753548824;
                color = COLOR_WHITE;
            }
            else if (sender == toolStripFFO45)
            {
                //FFO45
                bk = 282828816915486;
                wh = 9287318235258944;
                color = COLOR_BLACK;
            }
            else if (sender == toolStripFFO46)
            {
                //FFO46
                bk = 4052165999611379712;
                wh = 36117299622447104;
                color = COLOR_BLACK;
            }
            else if (sender == toolStripFFO47)
            {
                //FFO47
                bk = 277938752194568;
                wh = 3536224466208;
                color = COLOR_WHITE;
            }
            else if (sender == toolStripFFO48)
            {
                //FFO48
                bk = 38519958422848574;
                wh = 4725679339520;
                color = COLOR_WHITE;
            }
            else if (sender == toolStripFFO49)
            {
                //FFO49
                bk = 5765976742297600;
                wh = 4253833575484;
                color = COLOR_BLACK;
            }
            else if (sender == toolStripFFO50)
            {
                //FFO50
                bk = 4504145659822080;
                wh = 4336117619740130304;
                color = COLOR_BLACK;
            }
            else if (sender == toolStripFFO51)
            {
                //FFO51
                bk = 349834415978528;
                wh = 8664011788383158280;
                color = COLOR_WHITE;
            }
            else if (sender == toolStripFFO52)
            {
                //FFO52
                bk = 9096176176681728056;
                wh = 35409824317440;
                color = COLOR_WHITE;
            }
            else if (sender == toolStripFFO53)
            {
                //FFO53
                bk = 2515768979493888;
                wh = 8949795312300457984;
                color = COLOR_BLACK;
            }
            else if (sender == toolStripFFO54)
            {
                //FFO54
                bk = 26457201720894;
                wh = 289431515079835648;
                color = COLOR_BLACK;
            }
            else if (sender == toolStripFFO55)
            {
                //FFO55
                bk = 4635799596172290;
                wh = 289361502099486840;
                color = COLOR_WHITE;
            }
            else if (sender == toolStripFFO56)
            {
                //FFO56
                bk = 4925086697193472;
                wh = 9007372734053408;
                color = COLOR_WHITE;
            }
            else if (sender == toolStripFFO57)
            {
                //FFO57
                bk = 9060166336512000;
                wh = 8943248156475301888;
                color = COLOR_BLACK;
            }
            else if (sender == toolStripFFO58)
            {
                //FFO58
                bk = 4636039783186432;
                wh = 3383245044333600;
                color = COLOR_BLACK;
            }
            else 
            {
                // FFO59
                bk = 17320879491911778304;
                wh = 295223649004691488;
                color = COLOR_BLACK;
            }

            boardclass.InitBoard(color, bk, wh);
            nowColor = boardclass.GetNowColor();

            if (color == COLOR_BLACK)
            {
                comboBox1.SelectedIndex = 14;
                comboBox2.SelectedIndex = 0;
            }
            else 
            {
                comboBox1.SelectedIndex = 0;
                comboBox2.SelectedIndex = 14;
            }

            SetPlayerInfo();
            panel1.Invalidate(false);
            panel1.Update();
        }

        private void bestlineの表示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MenuDisplayPV.Checked)
            {
                m_enablePVdisplay = false;
                MenuDisplayPV.Checked = false;
            }
            else
            {
                m_enablePVdisplay = true;
                MenuDisplayPV.Checked = true;
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void 思考過程を表示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MenuAIThinking.Checked)
            {
                m_enableDetailDisplay = false;
                MenuAIThinking.Checked = false;
            }
            else
            {
                m_enableDetailDisplay = true;
                MenuAIThinking.Checked = true;
            }
        }

        private void 詳細結果の表示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MenuResultDetail.Checked)
            {
                m_enableResultDisplay = false;
                MenuResultDetail.Checked = false;
            }
            else
            {
                m_enableResultDisplay = true;
                MenuResultDetail.Checked = true;
            }
        }

        private void 度回転ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void hintToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = ((ToolStripMenuItem)sender);
            string text = menuItem.Text;

            if (m_game_mode == BATTLE_MODE)
            {
                menuItem.Text = "対戦モード";

                comboBox1.SelectedIndex = 0;
                comboBox2.SelectedIndex = 0;
                comboBox1.Enabled = false;
                comboBox2.Enabled = false;
                m_hintLevel = 6;
                m_hintList.Clear();
                m_mode = ON_GAME;
                m_game_mode = ANALYZE_MODE;
                m_hint_mode = ON_HINT;
                m_hintEvalMax = -INFINITY_SCORE;
                // ヒント処理ハンドラをコール
                m_hintFlagProperty = true;
            }
            else
            {
                menuItem.Text = "解析モード";
                m_mode = ON_GAME;
                m_game_mode = BATTLE_MODE;
                m_hint_mode = ON_NOTHING;
                m_hintEvalMax = -INFINITY_SCORE;
                // ヒント処理ハンドラをコール
                m_hintFlagProperty = false;
                panel1.Invalidate(false);
                panel1.Update();
            }
        }

        private void ConfigCasheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int size = CasheToolStripMenuItem.DropDownItems.Count;
            ToolStripMenuItem stripItem = ((ToolStripMenuItem)sender);

            // チェック全解除
            for (int i = 0; i < size; i++)
            {
                ((ToolStripMenuItem)CasheToolStripMenuItem.DropDownItems[i]).Checked = false;
            }

            uint casheSize = Convert.ToUInt32(stripItem.Text.Replace("MB", ""));

            // size(MB)--> size * 1024 * 1024 / sizeof(hash_entry) = size * 1024 * 16
            cpuClass[0].SetCasheSize(casheSize * 1024 * 16);
            cpuClass[1].SetCasheSize(casheSize * 1024 * 16);
            stripItem.Checked = true;
        }


        void SetControlEnable(bool flag)
        {
            button1.Enabled = flag;
            button2.Enabled = flag;
            button3.Enabled = flag;
            button4.Enabled = flag;
            button5.Enabled = flag;
            button7.Enabled = flag;
            comboBox1.Enabled = flag;
            comboBox2.Enabled = flag;

            menuStrip1.Enabled = flag;

        }

        private void 手番変更ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            boardclass.ChangeColor();
            SetPlayerInfo();
            ChangePlayer();

            if (boardclass.GetRecentTurn() == 0)
            {
                boardclass.InitBoard(nowColor, boardclass.GetBlack(), boardclass.GetWhite());
            }
        }

        private void 置換表のメモリを解放ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cppWrapper.ReleaseHash();
        }

        private void OnChangeBookToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int size = changeBookToolStripMenuItem.DropDownItems.Count;
            ToolStripMenuItem stripItem = ((ToolStripMenuItem)sender);

            // チェック全解除
            for (int i = 0; i < size; i++)
            {
                ((ToolStripMenuItem)changeBookToolStripMenuItem.DropDownItems[i]).Checked = false;
            }
            stripItem.Checked = true;

            uint idx = (uint)changeBookToolStripMenuItem.DropDownItems.IndexOf(stripItem);

            cpuClass[0].SetBookVariability(idx);
            cpuClass[1].SetBookVariability(idx);

        }

        private void bOOKのメモリを解放ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cppWrapper.ReleaseBook();
        }

    }
}
