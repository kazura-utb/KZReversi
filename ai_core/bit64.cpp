/***************************************************************************
* Name  : bit64.cpp
* Brief : 局面と定石データを照らし合わせて着手
* Date  : 2016/02/01
****************************************************************************/

#include "stdafx.h"
#include <intrin.h>
#include "bit64.h"

/***************************************************************************
* Name  : CountBit
* Brief : ビット列から１が立っているビットの数を数える
* Return: １が立っているビット数
****************************************************************************/
UINT32 CountBit(UINT64 bit)
{
	int l_moves = bit & 0x00000000FFFFFFFF;
	int h_moves = (bit & 0xFFFFFFFF00000000) >> 32;

	int count = _mm_popcnt_u32(l_moves);
	count += _mm_popcnt_u32(h_moves);

	return count;
}