namespace ChartForgeX.Raster;

internal static class TinyFont {
    public static byte[] Get(char c) => c switch {
        '0' => new byte[]{7,5,5,5,7}, '1' => new byte[]{2,6,2,2,7}, '2' => new byte[]{7,1,7,4,7}, '3' => new byte[]{7,1,7,1,7},
        '4' => new byte[]{5,5,7,1,1}, '5' => new byte[]{7,4,7,1,7}, '6' => new byte[]{7,4,7,5,7}, '7' => new byte[]{7,1,1,1,1},
        '8' => new byte[]{7,5,7,5,7}, '9' => new byte[]{7,5,7,1,7}, 'A' => new byte[]{7,5,7,5,5}, 'B' => new byte[]{6,5,6,5,6},
        'C' => new byte[]{7,4,4,4,7}, 'D' => new byte[]{6,5,5,5,6}, 'E' => new byte[]{7,4,6,4,7}, 'F' => new byte[]{7,4,6,4,4},
        'G' => new byte[]{7,4,5,5,7}, 'H' => new byte[]{5,5,7,5,5}, 'I' => new byte[]{7,2,2,2,7}, 'J' => new byte[]{1,1,1,5,7},
        'K' => new byte[]{5,5,6,5,5}, 'L' => new byte[]{4,4,4,4,7}, 'M' => new byte[]{5,7,7,5,5}, 'N' => new byte[]{5,7,7,7,5},
        'O' => new byte[]{7,5,5,5,7}, 'P' => new byte[]{7,5,7,4,4}, 'Q' => new byte[]{7,5,5,7,1}, 'R' => new byte[]{7,5,7,6,5},
        'S' => new byte[]{7,4,7,1,7}, 'T' => new byte[]{7,2,2,2,2}, 'U' => new byte[]{5,5,5,5,7}, 'V' => new byte[]{5,5,5,5,2},
        'W' => new byte[]{5,5,7,7,5}, 'X' => new byte[]{5,5,2,5,5}, 'Y' => new byte[]{5,5,2,2,2}, 'Z' => new byte[]{7,1,2,4,7},
        '-' => new byte[]{0,0,7,0,0}, '.' => new byte[]{0,0,0,0,2}, ':' => new byte[]{0,2,0,2,0}, '%' => new byte[]{5,1,2,4,5}, ' ' => new byte[]{0,0,0,0,0},
        _ => new byte[]{7,1,2,0,2}
    };
}
