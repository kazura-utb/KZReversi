﻿//! @file
//! CPU メソッド群
//****************************************************************************
//       (c) COPYRIGHT Kazuho Nagata 2016-  All Rights Reserved.
//****************************************************************************
// FILE NAME     : CpuClass.cs
// PROGRAM NAME  : KZReversi
// FUNCTION      : CPUの処理全般
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
//│ A  │2016/02/01│新規作成                            │Kazuho Nagata │
//└──┴─────┴──────────────────┴───────┘
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace KZreversi
{
    public class CpuClass
    {
        private CpuConfig cConfig;
        private ulong moves;

        public CpuClass() 
        {
            cConfig = new CpuConfig();
        }

        public void StartCpuThread(BoardClass boardclass, Form1 context)
        {
            object[] args = new object[2];

            args[0] = boardclass;
            args[1] = context;

            Thread th = new Thread(new ParameterizedThreadStart(CpuFunc));
            th.Start(args);
        }


        private void CpuFunc(object args) 
        {
            object[] argArray = (object[])args;
            BoardClass boardclass = (BoardClass)argArray[0];
            object formobj = argArray[1];

            CppWrapper cp = new CppWrapper();
            ulong bk = boardclass.GetBlack();
            ulong wh = boardclass.GetWhite();
            moves = cp.GetCpuMove(bk, wh, cConfig);

            // Form1のプロパティにCPUの着手を設定
            ((Form1)formobj).Invoke(((Form1)formobj).delegateObj, new object[] { moves });

        }

        public uint GetColor()
        {
            return cConfig.color;
        }

        public void SetColor(uint color)
        {
            cConfig.color = color;
        }

        public uint GetCasheSize()
        {
            return cConfig.casheSize;
        }

        public void SetCasheSize(uint casheSize)
        {
            cConfig.casheSize = casheSize;
        }

        public uint GetSearchDepth()
        {
            return cConfig.searchDepth;
        }

        public void SetSearchDepth(uint searchDepth)
        {
            cConfig.searchDepth = searchDepth;
        }

        public uint GetWinLossDepth()
        {
            return cConfig.winLossDepth;
        }

        public void SetWinLossDepth(uint winLossDepth)
        {
            cConfig.winLossDepth = winLossDepth;
        }

        public uint GetExactDepth()
        {
            return cConfig.exactDepth;
        }

        public void SetExactDepth(uint exactDepth)
        {
            cConfig.exactDepth = exactDepth;
        }

        public bool GetBookFlag()
        {
            return cConfig.bookFlag;
        }

        public void SetBookFlag(bool bookFlag)
        {
            cConfig.bookFlag = bookFlag;
        }

        public uint GetBookVariability()
        {
            return cConfig.bookVariability;
        }

        public void SetBookVariability(uint bookVariability)
        {
            cConfig.bookVariability = bookVariability;
        }

        public bool GetMpcFlag()
        {
            return cConfig.mpcFlag;
        }

        public void SetMpcFlag(bool mpcFlag)
        {
            cConfig.mpcFlag = mpcFlag;
        }

        public bool GetTableFlag()
        {
            return cConfig.tableFlag;
        }

        public void SetTableFlag(bool tableFlag)
        {
            cConfig.tableFlag = tableFlag;
        }


    }
}