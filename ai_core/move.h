/***************************************************************************
* Name  : move.h
* Brief : ����֘A�̌v�Z���s��
* Date  : 2016/02/01
****************************************************************************/

#include "stdafx.h"

UINT64 CreateMoves(UINT64 bk_p, UINT64 wh_p, UINT32 *p_count_p);
UINT64 GetPotentialMoves(UINT64 P, UINT64 O, UINT64 blank);