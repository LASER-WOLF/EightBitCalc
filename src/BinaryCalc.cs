﻿class BinaryCalc
{
    // Set global enums
    private enum STATE 
    {
        MAIN,
        HELP,
        SETTINGS,
        ASSEMBLY,
        ERROR,
    };

    private enum MAIN_SUB_STATE 
    {
        DEFAULT,
        ADD,
        SUBTRACT
    };

    private enum ByteType 
    {
        Binary,
        Decimal,
        Hex,
        Address
    };

    // Set global constants
    private const char EMPTY = ' ';
    private const char FILLED = '#';
    private const int CHAR_SPACING = 2;
    private const int CHAR_HEIGHT = 7;
    private const int CHAR_WIDTH = 5 + CHAR_SPACING;
    private const int CHAR_LIMIT = 8;
    private bool[,,] DIGITS = new bool[2,CHAR_HEIGHT,CHAR_WIDTH];
    private const string MAIN_TITLE = "8-BIT BINARY CALCULATOR";
    private const int RENDER_WIDTH = CHAR_LIMIT * CHAR_WIDTH;
    private const int RENDER_HEIGHT = 48;
    private const int REGISTER_NUM = 3;
    private const int MEMORY_NUM = 8;

    // Set global variables
    private bool running = true;
    private STATE appState = STATE.MAIN;
    private STATE appStatePrev = STATE.MAIN;
    private MAIN_SUB_STATE mainSubState = MAIN_SUB_STATE.DEFAULT;
    private string[] display = new string[CHAR_HEIGHT];
    private byte[] registers = new byte[REGISTER_NUM];
    private byte[] registersPrev = new byte[REGISTER_NUM];
    private byte[] memory = new byte[MEMORY_NUM];
    private byte registerIndex = 0;
    private bool carryFlag = false;
    private bool zeroFlag = false;
    private bool negativeFlag = false;
    private bool overflowFlag = false;
    private int windowWidth = 0;
    private int windowHeight = 0;

    // Declare logger
    public List<string> messages = new List<string>();
    public List<string> assembly = new List<string>();

    // Settings
    private Dictionary<string, Setting> settings = new Dictionary<string, Setting>
    {
        { "uiHlChangedBit", new Setting(true,  "HIGHLIGHT BITS CHANGED IN PREVIOUS OPERATION") },
        { "flagsAutoCarry", new Setting(false, "AUTOMATICALLY SET/UNSET CARRY FLAG") },
        { "loggerAsmFile",  new Setting(false,  "(NOT IMPLEMENTED!) SAVE ASM FILE") }
    }; 

    // Dictionary for HELP content
    List <string> help = new List<string>();
    
    // Create scroll containers
    Dictionary<STATE, ContainerScroll> containerScroll = new Dictionary<STATE, ContainerScroll>
    {
        { STATE.HELP,     new ContainerScroll("HELP SCREEN",  RENDER_HEIGHT - 7) },
        { STATE.ASSEMBLY, new ContainerScroll("ASSEMBLY LOG", RENDER_HEIGHT - 7, startAtBottom: true, showLineNum: true) }
    };

    // Create toggle containers
    Dictionary<STATE, ContainerToggle> containerToggle = new Dictionary<STATE, ContainerToggle>
    {
        { STATE.SETTINGS, new ContainerToggle("SETTINGS", RENDER_HEIGHT - 7) }
    };

    // Set error types
    Dictionary<string, bool> appError = new Dictionary<string, bool>
    {
        { "windowSize",     false }
    };

    // Change the app STATE
    private void ChangeState(STATE newState)
    {
        appStatePrev = appState;
        appState = newState;
        mainSubState = MAIN_SUB_STATE.DEFAULT;

        // Reset sel position for container
        if (containerScroll.ContainsKey(newState)) { containerScroll[newState].Zero(); }
        else if (containerToggle.ContainsKey(newState)) { containerToggle[newState].Zero(); }
    }

    private void ChangeMainSubState(MAIN_SUB_STATE newSubState)
    {
        mainSubState = newSubState;
    }

    // Check console window size in rows and columns
    private void CheckWindowSize()
    {
        if (Console.WindowWidth != windowWidth || Console.WindowHeight != windowHeight)
        {
            // Update the window size vars
            windowWidth = Console.WindowWidth;
            windowHeight = Console.WindowHeight;

            // Check if the window is too small
            if (windowWidth < RENDER_WIDTH || windowHeight < RENDER_HEIGHT)
            {
                ChangeState(STATE.ERROR);
                appError["windowSize"] = true;
            }
            else 
            {
                ChangeState(appStatePrev);
                appError["windowSize"] = false;
            }
        }
    }

    // Add message to the message log
    public void AddMessage(string line)
    {
        messages.Add(DateTime.Now.ToString("HH:mm:ss") + " " + line.ToUpper());
    }

    // Add assembly code to the assembly log
    public void AddAssembly(string line)
    {
        containerScroll[STATE.ASSEMBLY].AddContent(line);
    }

    // Render message log
    public void RenderMessages(int num = 8)
    {
        for (int i = num; i >= 0; i--)
        {
            Console.WriteLine(messages.Count > i ? " " + messages[messages.Count-(i+1)] : " -");
        }
    }

    // Convert a byte to an eight digit string string
    private string ByteToString(byte bits)
    {
        return Convert.ToString(bits, 2).PadLeft(8, '0');
    }
    
    // Get the letter for a given register
    private char GetRegisterChar(byte index)
    {
        //return (char)((int)'A' + index);
        return (char)((int)'A' + (index == 0 ? 0 : index + 22));
    }

    // Convert numerical value to hex value
    public string HexToString(int hex)
    {
        return hex.ToString("X2");
    }

    // Initialize values
    private void Init() 
    {
        // Generate blank line and seperator lines
        Lines.Init(RENDER_WIDTH);

        // Zero digit
        DIGITS[0,1,0] = true;
        DIGITS[0,2,0] = true;
        DIGITS[0,3,0] = true;
        DIGITS[0,4,0] = true;
        DIGITS[0,5,0] = true;
        DIGITS[0,0,1] = true;
        DIGITS[0,6,1] = true;
        DIGITS[0,0,2] = true;
        DIGITS[0,6,2] = true;
        DIGITS[0,0,3] = true;
        DIGITS[0,6,3] = true;
        DIGITS[0,1,4] = true;
        DIGITS[0,2,4] = true;
        DIGITS[0,3,4] = true;
        DIGITS[0,4,4] = true;
        DIGITS[0,5,4] = true;
        //DIGITS[0,3,2] = true;

        // One digit
        DIGITS[1,0,2] = true;
        DIGITS[1,1,1] = true;
        DIGITS[1,1,2] = true;
        DIGITS[1,2,2] = true;
        DIGITS[1,3,2] = true;
        DIGITS[1,4,2] = true;
        DIGITS[1,5,2] = true;
        DIGITS[1,6,1] = true;
        DIGITS[1,6,2] = true;
        DIGITS[1,6,3] = true;

        // Set HELP text
        help = new List<string> 
        {
            "",
            " TODO: ... ABOUT BINARY, BITS AND BYTES...",
            " (ADD TEXT HERE ..)",
            "",
            " BASE-2 (BINARY) TO BASE-10 (DECIMAL):",
            "  128's  64's  32's  16's   8's   4's   2's   1's",
            "    |     |     |     |     |     |     |     |",
            "    0     0     0     0     0     1     1     1  =  7",
            "    0     1     1     1     1     1     1     1  =  127",
            "    1     0     0     0     0     0     0     1  =  128",
            "    1     1     1     1     1     1     1     1  =  255",
            "",
            " NEGATIVE NUMBERS USING TWO\'S COMPLIMENT:",
            " -128's  64's  32's  16's   8's   4's   2's   1's",
            "    |     |     |     |     |     |     |     |",
            "    0     0     0     0     0     1     1     1  =  7",
            "    0     1     1     1     1     1     1     1  =  127",
            "    1     0     0     0     0     0     0     1  = -128",
            "    1     1     1     1     1     1     1     1  = -1",
            "",
            Lines.seperator[1],
            "",
            " " + COLORS.UL + "ADC: ADD WITH CARRY" + COLORS.DEFAULT + "                          (N,V,Z,C)",
            " ADDS THE VALUE OF A CHOSEN REGISTER [A] - [H]",
            " TO THE CURRENT REGISTER.",
            " THE (C)ARRY FLAG IS SET IF THE RESULTING VALUE",
            " IS ABOVE 255 (UNSIGNED).", 
            " THE O(V)ERFLOW FLAG IS SET IF ADDING TO A POSITIVE",
            " NUMBER AND ENDING UP WITH A NEGATIVE NUMBER.",
            "",
            " " + COLORS.UL + "SBC: SUBTRACT WITH CARRY" + COLORS.DEFAULT + "                     (N,V,Z,C)",
            " SUBTRACTS THE VALUE OF A CHOSEN REGISTER [A] - [H]",
            " FROM THE CURRENT REGISTER.",
            " THE (C)ARRY FLAG IS CLEAR IF THE RESULTING VALUE",
            " IS LESS THAN 0 (UNSIGNED).", 
            " THE O(V)ERFLOW FLAG IS SET IF SUBTRACTING FROM A",
            " NEGATIVE NUMBER AND ENDING UP WITH A POSITIVE NUMBER.",
            "",
            " " + COLORS.UL + "INC: INCREMENT" + COLORS.DEFAULT + "                               (N,Z)",
            " ADDS ONE TO THE VALUE OF THE CURRENT REGISTER.",
            "",
            " " + COLORS.UL + "DEC: DECREMENT" + COLORS.DEFAULT + "                               (N,Z)",
            " SUBTRACTS ONE FROM THE VALUE OF THE CURRENT REGISTER.",
            "",
            " " + COLORS.UL + "ASL: ARITHMETIC SHIFT LEFT" + COLORS.DEFAULT + "                   (N,Z,C)",
            " MOVES ALL BITS ONE STEP TO THE LEFT",
            " INSERTING A 0 IN THE RIGHTMOST BIT",
            " AND MOVING THE LEFTMOST BIT TO THE (C)ARRY FLAG.",
            " THIS OPERATION IS EQUIVALENT TO MULTIPLYING BY 2.",
            "",
            " " + COLORS.UL + "LSR: LOGICAL SHIFT RIGHT" + COLORS.DEFAULT + "                     (N,Z,C)",
            " MOVES ALL BITS ONE STEP TO THE RIGHT",
            " INSERTING A 0 IN THE LEFTMOST BIT",
            " AND MOVING THE RIGHTMOST BIT TO THE (C)ARRY FLAG.",
            " THIS OPERATION IS EQUIVALENT TO DIVIDING BY 2.",
            "",
            " " + COLORS.UL + "ROL: ROTATE LEFT" + COLORS.DEFAULT + "                             (N,Z,C)",
            " MOVES ALL BITS ONE STEP TO THE LEFT",
            " THE LEFTMOST BIT MOVES OVER TO THE RIGHTMOST SIDE.",
            "",
            " " + COLORS.UL + "ROR: ROTATE RIGHT" + COLORS.DEFAULT + "                            (N,Z,C)",
            " MOVES ALL BITS ONE STEP TO THE RIGHT",
            " THE RIGHTMOST BIT MOVES OVER TO THE LEFTMOST SIDE.",
            "",
            " " + COLORS.UL + "AND: LOGICAL AND" + COLORS.DEFAULT + "                             (N,Z)",
            " THE RESULT OF A LOGICAL AND IS ONLY TRUE",
            " IF BOTH INPUTS ARE TRUE.",
            " CAN BE USED TO MASK BITS.",
            " CAN BE USED TO CHECK FOR EVEN/ODD NUMBERS.",
            " CAN BE USED TO CHECK IF A NUMBER IS",
            " DIVISIBLE BY 2/4/6/8 ETC.",
            "",
            " " + COLORS.UL + "EOR: EXCLUSIVE OR" + COLORS.DEFAULT + "                            (N,Z)",
            " AN EXCLUSIVE OR IS SIMILAR TO LOGICAL OR, WITH THE",
            " EXCEPTION THAT IS IS FALSE WHEN BOTH INPUTS ARE TRUE.",
            " EOR CAN BE USED TO FLIP BITS.",
            "",
            " " + COLORS.UL + "ORA: LOGICAL INCLUSIVE OR" + COLORS.DEFAULT + "                    (N,Z)",
            " THE RESULT OF A LOGICAL INCLUSIVE OR IS TRUE IF",
            " AT LEAST ONE OF THE INPUTS ARE TRUE.",
            " ORA CAN BE USED TO SET A PARTICULAR BIT TO TRUE.",
            " ORA + EOR CAN BE USED TO SET A PARTICULAR BIT TO FALSE.",
            "",
            Lines.seperator[1],
            "",
            " FLAGS ARE SET AFTER PERFORMING OPERATIONS.",
            " FLAGS ARE INDICATED BY PARANTHESIS.",
            "",
            " FOUR FLAGS ARE IMPLEMENTED:",
            " (C) CARRY FLAG",
            " (Z) ZERO FLAG:     IS SET TO 1 IF THE RESULT IS ZERO",
            " (O) OVERFLOW FLAG",
            " (N) NEGATIVE FLAG: IS SET TO 1 IF BIT 7 IS 1",
            "",
            Lines.seperator[1],
            "",
            " A REGISTER HOLDS ONE BYTE OF DATA.",
            " REGISTERS ARE INDICATED BY SQUARE BRACKETS.",
            " EIGHT REGISTERS [A] TO [H] ARE IMPLEMENTED.",
            "",
            Lines.seperator[1],
            "",
            " KEYBINDINGS AND MODIFIERS:",
            " <KEY>",
            " ...",
            "",
        };

        // Link lists with containers
        containerScroll[STATE.HELP].LinkContent(help);
        containerScroll[STATE.ASSEMBLY].LinkContent(assembly);
        containerToggle[STATE.SETTINGS].LinkContent(settings);

        AddMessage("Welcome!");
    }

    // Unset all flags
    private void UnsetFlags()
    {
        zeroFlag = false;
        negativeFlag = false;
        overflowFlag = false;
    }

    // Set zero flag if value is zero
    private void SetZeroFlag() 
    {
        if (registers[registerIndex] == 0b00000000) { zeroFlag = true; }
    }

    // Set negative flag if bit 7 is set
    private void SetNegativeFlag()
    {
        if ((registers[registerIndex] & 0b10000000) == 0b10000000) { negativeFlag = true; }
    }
   
    // Set the (C)arry flag to 0
    private void CLC(bool silent = false)
    {
        carryFlag = false;
        if (!silent) { AddMessage("CLC: CLEAR CARRY FLAG"); }
        AddAssembly("CLC");
    }
    
    // Set the (C)arry flag to 1
    private void SEC(bool silent = false)
    {
        carryFlag = true;
        if (!silent) { AddMessage("SEC: SET CARRY FLAG"); }
        AddAssembly("SEC");
    }

    // Change active register
    private void ChangeRegister(byte targetRegister)
    {
        registerIndex = targetRegister;
    }

    // Set the o(V)erflow flag to 0
    private void CLV(bool silent = false)
    {
        overflowFlag = false;
        if (!silent) { AddMessage("CLV: CLEAR OVERFLOW FLAG"); }
        AddAssembly("CLV");
    }

    // Logical AND operation
    private void AND(byte bits, bool silent = false)
    {
        registersPrev[0] = registers[0];
        UnsetFlags();
        registers[0] = (byte)(registers[0] & bits);
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "AND: Logical AND              <- " + ByteToString(bits)); }
        AddAssembly("AND #%" + ByteToString(bits)); 
    } 

    // Logical inclusive OR operation
    private void ORA(byte bits, bool silent = false)
    {
        registersPrev[0] = registers[0];
        UnsetFlags();
        registers[0] = (byte)(registers[0] | bits);
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "ORA: Logical Inclusive OR     <- " + ByteToString(bits)); }
        AddAssembly("AND #%" + ByteToString(bits)); 
    }

    // Load A operation
    private void LDA(int position, bool silent = false)
    {
        registersPrev[0] = registers[0];
        UnsetFlags();
        registers[0] = memory[position];
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "LDA: Load Accumulator         <- " + ByteToString(memory[position])); }
        AddAssembly("LDA $" + HexToString(position)); 
    }

    // Load X operation
    private void LDX(byte position, bool silent = false)
    {
        registersPrev[1] = registers[1];
        UnsetFlags();
        registers[1] = memory[position];
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(1) + "] " + "LDX: Load X Register          <- " + ByteToString(memory[position])); }
        AddAssembly("LDX $" + HexToString(position)); 
    }

    // Load Y operation
    private void LDY(byte position, bool silent = false)
    {
        registersPrev[2] = registers[2];
        UnsetFlags();
        registers[2] = memory[position];
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(2) + "] " + "LDY: Load Y Register          <- " + ByteToString(memory[position])); }
        AddAssembly("LDY $" + HexToString(position)); 
    }

    // Store A operation
    private void STA(byte position, bool silent = false)
    {
        memory[position] = registers[0];
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "STA: Store Accumulator        <- " + ByteToString(memory[position])); }
        AddAssembly("STA $" + HexToString(position)); 
    }

    // Store X operation
    private void STX(byte position, bool silent = false)
    {
        memory[position] = registers[1];
        if (!silent) { AddMessage("[" + GetRegisterChar(1) + "] " + "STX: Store X Register         <- " + ByteToString(memory[position])); }
        AddAssembly("STX $" + HexToString(position)); 
    }

    // Store Y operation
    private void STY(byte position, bool silent = false)
    {
        memory[position] = registers[2];
        if (!silent) { AddMessage("[" + GetRegisterChar(2) + "] " + "STY: Store Y Register         <- " + ByteToString(memory[position])); }
        AddAssembly("STY $" + HexToString(position)); 
    }

    // Transfer A to X operation
    private void TAX(bool silent = false)
    {
        registersPrev[1] = registers[1];
        UnsetFlags();
        registers[1] = registers[0];
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(1) + "] " + "TAX: Transfer A to X          <- " + ByteToString(registers[0])); }
        AddAssembly("TAX"); 
    }

    // Transfer A to Y operation
    private void TAY(bool silent = false)
    {
        registersPrev[2] = registers[2];
        UnsetFlags();
        registers[2] = registers[0];
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(2) + "] " + "TAY: Transfer A to Y          <- " + ByteToString(registers[0])); }
        AddAssembly("TAY"); 
    }

    // Transfer X to A operation
    private void TXA(bool silent = false)
    {
        registersPrev[0] = registers[0];
        UnsetFlags();
        registers[0] = registers[1];
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "TXA: Transfer X to A          <- " + ByteToString(registers[1])); }
        AddAssembly("TXA"); 
    }

    // Transfer Y to A operation
    private void TYA(bool silent = false)
    {
        registersPrev[0] = registers[0];
        UnsetFlags();
        registers[0] = registers[2];
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "TYA: Transfer Y to A          <- " + ByteToString(registers[2])); }
        AddAssembly("TYA"); 
    }

    // Exclusive OR operation
    private void EOR(byte bits, bool silent = false)
    {
        registersPrev[0] = registers[0];
        UnsetFlags();
        registers[0] = (byte)(registers[0] ^ bits);
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "EOR: Exclusive OR             <- " + ByteToString(bits)); }
        AddAssembly("EOR #%" + ByteToString(bits));
    }

    // Arithmetic shift left operation
    private void ASL(bool silent = false)
    {
        registersPrev[0] = registers[0];
        UnsetFlags();
        if ((registers[0] & 0b10000000) == 0b10000000) { carryFlag = true; }
        registers[0] = (byte)(registers[0] << 1);
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "ASL: Arithmetic shift left"); }
        AddAssembly("ASL");
    }

    // Logical shift right operation
    private void LSR(bool silent = false)
    {
        registersPrev[0] = registers[0];
        UnsetFlags();
        if ((registers[0] & 0b00000001) == 0b00000001) {carryFlag = true;}
        registers[0] = (byte)(registers[0] >> 1);
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "LSR: Logical shift right"); }
        AddAssembly("LSR");
    }

    // Rotate left operation
    private void ROL(bool silent = false)
    {
        registersPrev[0] = registers[0];
        UnsetFlags();
        if ((registers[0] & 0b10000000) == 0b10000000) { carryFlag = true; }
        registers[0] = (byte)(registers[0] << 1);
        if (carryFlag) { registers[0] += 0b00000001; }
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "ROL: Rotate left"); }
        AddAssembly("ROL");
    }
    
    // Rotate right operation
    private void ROR(bool silent = false)
    {
        registersPrev[0] = registers[0];
        UnsetFlags();
        if ((registers[0] & 0b00000001) == 0b00000001) {carryFlag = true;}
        registers[0] = (byte)(registers[0] >> 1);
        if (carryFlag) { registers[0] += 0b10000000; }
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "ROR: Rotate right"); }
        AddAssembly("ROR");
    }

    // Decrement X operation
    private void DEX(bool silent = false)
    {
        registersPrev[1] = registers[1];
        UnsetFlags();
        registers[1] -= (byte)0b00000001;
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(1) + "] " + "DEX: Decrement X"); }
        AddAssembly("DEX");
    }
    
    // Decrement Y operation
    private void DEY(bool silent = false)
    {
        registersPrev[2] = registers[2];
        UnsetFlags();
        registers[2] -= (byte)0b00000001;
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(2) + "] " + "DEY: Decrement Y"); }
        AddAssembly("DEY");
    }
    
    // Increment X operation
    private void INX(bool silent = false)
    {
        registersPrev[1] = registers[1];
        UnsetFlags();
        registers[1] += (byte)0b00000001;
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(1) + "] " + "INX: Increment X"); }
        AddAssembly("INX");
    }
    
    // Increment operation
    private void INY(bool silent = false)
    {
        registersPrev[2] = registers[2];
        UnsetFlags();
        registers[2] += (byte)0b00000001;
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(2) + "] " + "INY: Increment Y"); }
        AddAssembly("INY");
    }

    // Add with carry operation
    private void ADC(byte bits, ByteType type = ByteType.Binary, bool silent = false, bool inc = false)
    {
        string asm = "ADC ";
        if (type == ByteType.Binary) { asm += "#%" + ByteToString(bits); }
        else if (type == ByteType.Decimal) { asm += "#" + bits.ToString(); }
        else if (type == ByteType.Hex) {asm += "#$" + HexToString(bits);}
        else if (type == ByteType.Address) {asm += "$" + HexToString(bits); bits = memory[bits];}

        registersPrev[0] = registers[0];
        UnsetFlags();
        if (settings["flagsAutoCarry"].enabled && carryFlag) { CLC(); }
        if (carryFlag && inc) { bits = 0b00000000; }
        byte bitsWithCarry = (byte)(bits + (byte)(carryFlag ? 0b00000001 : 0b00000000));
        if (((registers[0] & 0b10000000) == 0b10000000) && (((registers[0] + bits) & 0b10000000) == 0b00000000)) { carryFlag = true; }
        if (((registers[0] & 0b10000000) == 0b00000000) && (((registers[0] + bits) & 0b10000000) == 0b10000000)) { overflowFlag = true; }
        registers[0] += bitsWithCarry;
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "ADC: Add with carry           <- " + ByteToString(bitsWithCarry) ); }
        AddAssembly(asm);
    }

    // Subtract with carry operation
    private void SBC(byte bits, ByteType type = ByteType.Binary, bool silent = false, bool dec = false)
    {
        string asm = "SBC ";
        if (type == ByteType.Binary) { asm += "#%" + ByteToString(bits); }
        else if (type == ByteType.Decimal) { asm += "#" + bits.ToString(); }
        else if (type == ByteType.Hex) {asm += "#$" + HexToString(bits);}
        else if (type == ByteType.Address) {asm += "$" + HexToString(bits); bits = memory[bits];}
        
        registersPrev[0] = registers[0];
        UnsetFlags();
        if (settings["flagsAutoCarry"].enabled && !carryFlag) { SEC(); }
        if (!carryFlag && dec) { bits = 0b00000000; }
        byte bitsWithCarry = (byte)(bits + (byte)(carryFlag ? 0b00000000 : 0b00000001));
        if (registers[0] - bits < 0) { carryFlag = false; }
        if (((registers[0] & 0b10000000) == 0b00000000) && (((registers[0] - bits) & 0b10000000) == 0b10000000)) { overflowFlag = true; }
        registers[0] -= bitsWithCarry;
        SetZeroFlag();
        SetNegativeFlag();
        if (!silent) { AddMessage("[" + GetRegisterChar(0) + "] " + "SBC: Subtract with carry      <- " + ByteToString(bitsWithCarry) ); }
        AddAssembly(asm);
    }

    private void Increment()
    {
        switch (registerIndex)
        {
            case 0:
                ADC((byte)(0b00000001), inc: true);
                break;
            case 1:
                INX();
                break;
            case 2:
                INY();
                break;
        }
    }
    
    private void Decrement()
    {
        switch (registerIndex)
        {
            case 0:
                SBC((byte)(0b00000001), dec: true);
                break;
            case 1:
                DEX();
                break;
            case 2:
                DEY();
                break;
        }
    }

    private bool HandleInputNum(byte memoryIndex, bool keyMod)
    {
        // Load value to register from memory
        if (keyMod)
        {
            if (registerIndex == 0) { LDA(memoryIndex); }
            else if (registerIndex == 1) { LDX(memoryIndex); }
            else if (registerIndex == 2) { LDY(memoryIndex); }
        }

        // Store value to memory from register
        else
        {
            if (registerIndex == 0) { STA(memoryIndex); }
            else if (registerIndex == 1) { STX(memoryIndex); }
            else if (registerIndex == 2) { STY(memoryIndex); }
        }
        
        return true;
    }

    private bool HandleInputReg(byte targetRegisterIndex, bool keyMod)
    {
        if (targetRegisterIndex != registerIndex)
        {
            if (keyMod)
            {
                if (registerIndex == 0) 
                {
                    if (targetRegisterIndex == 1) { TAX(); return true; }
                    else if (targetRegisterIndex == 2) { TAY(); return true; }
                }
                else if (registerIndex == 1)
                {
                    if (targetRegisterIndex == 0) { TXA(); return true; }
                }
                else if (registerIndex == 2)
                {
                    if (targetRegisterIndex == 0) { TYA(); return true; }
                }
            }
            else
            {
                ChangeRegister(targetRegisterIndex);
                return true;
            }
        }
        return false;
    }
    
    private bool GetInputMainAdd(ConsoleKeyInfo key, bool keyMod)
    {
        ChangeMainSubState(MAIN_SUB_STATE.DEFAULT);
        switch (key.Key)
        {
            case ConsoleKey.D0:
                ADC(0, ByteType.Address);
                return true;
            case ConsoleKey.D1:
                ADC(1, ByteType.Address);
                return true;
            case ConsoleKey.D2:
                ADC(2, ByteType.Address);
                return true;
            case ConsoleKey.D3:
                ADC(3, ByteType.Address);
                return true;
            case ConsoleKey.D4:
                ADC(4, ByteType.Address);
                return true;
            case ConsoleKey.D5:
                ADC(5, ByteType.Address);
                return true;
            case ConsoleKey.D6:
                ADC(6, ByteType.Address);
                return true;
            case ConsoleKey.D7:
                ADC(7, ByteType.Address);
                return true;
        }
        return false;
    }
    
    private bool GetInputMainSubtract(ConsoleKeyInfo key, bool keyMod)
    {
        ChangeMainSubState(MAIN_SUB_STATE.DEFAULT);
        switch (key.Key)
        {
            case ConsoleKey.D0:
                SBC(0, ByteType.Address);
                return true;
            case ConsoleKey.D1:
                SBC(1, ByteType.Address);
                return true;
            case ConsoleKey.D2:
                SBC(2, ByteType.Address);
                return true;
            case ConsoleKey.D3:
                SBC(3, ByteType.Address);
                return true;
            case ConsoleKey.D4:
                SBC(4, ByteType.Address);
                return true;
            case ConsoleKey.D5:
                SBC(5, ByteType.Address);
                return true;
            case ConsoleKey.D6:
                SBC(6, ByteType.Address);
                return true;
            case ConsoleKey.D7:
                SBC(7, ByteType.Address);
                return true;
        }
        return false;
    }

    // Handle input in the MAIN screen
    private bool GetInputMain(ConsoleKeyInfo key, bool keyMod)
    {
        // Check if the pressed key is a valid key
        switch (key.Key)
        {
            case ConsoleKey.Add:
                if (registerIndex == 0) { ChangeMainSubState(MAIN_SUB_STATE.ADD); return true; }
                return false;
            case ConsoleKey.Subtract:
                if (registerIndex == 0) { ChangeMainSubState(MAIN_SUB_STATE.SUBTRACT); return true; }
                return false;
            case ConsoleKey.C:
                if (carryFlag) { CLC(); }
                else { SEC(); }
                return true;
            case ConsoleKey.V:
                if (overflowFlag) { CLV(); return true; }
                return false;
            case ConsoleKey.A:
                return HandleInputReg(0, keyMod);
            case ConsoleKey.X:
                return HandleInputReg(1, keyMod);
            case ConsoleKey.Y:
                return HandleInputReg(2, keyMod);
            case ConsoleKey.D0:
                return HandleInputNum(0,keyMod);
            case ConsoleKey.D1:
                return HandleInputNum(1,keyMod);
            case ConsoleKey.D2:
                return HandleInputNum(2,keyMod);
            case ConsoleKey.D3:
                return HandleInputNum(3,keyMod);
            case ConsoleKey.D4:
                return HandleInputNum(4,keyMod);
            case ConsoleKey.D5:
                return HandleInputNum(5,keyMod);
            case ConsoleKey.D6:
                return HandleInputNum(6,keyMod);
            case ConsoleKey.D7:
                return HandleInputNum(7,keyMod);
            case ConsoleKey.LeftArrow:
                if (registerIndex == 0) 
                { 
                    if (keyMod) { ROL(); } 
                    else { ASL(); }
                    return true;
                }
                return false;
            case ConsoleKey.RightArrow:
                if (registerIndex == 0) 
                { 
                    if (keyMod) { ROR(); } 
                    else { LSR(); }
                    return true;
                }
                return false;
            case ConsoleKey.UpArrow:
                Increment();
                return true;
            case ConsoleKey.DownArrow:
                Decrement();
                return true;
            case ConsoleKey.L:
                ChangeState(STATE.ASSEMBLY);
                return true;
            case ConsoleKey.S:
                ChangeState(STATE.SETTINGS);
                return true;
            case ConsoleKey.Escape:
                running = false;
                return true;
        }
        switch (key.KeyChar)
        {
            case '?':
                ChangeState(STATE.HELP);
                return true;
        }

        // No valid input
        return false;
    }

    // Handle input in the HELP screen
    private bool GetInputHelp(ConsoleKeyInfo key, bool keyMod)
    {
        // Check if the pressed key is a valid key
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                return containerScroll[STATE.HELP].Up();
            case ConsoleKey.DownArrow:
                return containerScroll[STATE.HELP].Down();
            case ConsoleKey.Escape:
                ChangeState(STATE.MAIN);
                return true;
        }

        // No valid input
        return false;
    }

    // Handle input in the SETTINGS screen
    private bool GetInputSettings(ConsoleKeyInfo key, bool keyMod)
    {
        // Check if the pressed key is a valid key
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                return containerToggle[STATE.SETTINGS].Up();
            case ConsoleKey.DownArrow:
                return containerToggle[STATE.SETTINGS].Down();
            case ConsoleKey.LeftArrow:
                return containerToggle[STATE.SETTINGS].Toggle();
            case ConsoleKey.RightArrow:
                return containerToggle[STATE.SETTINGS].Toggle();
            case ConsoleKey.Escape:
                ChangeState(STATE.MAIN);
                return true;
        }

        // No valid input
        return false;
    }

    // Handle input in the MESSAGES screen
    private bool GetInputAssembly(ConsoleKeyInfo key, bool keyMod)
    {
        // Check if the pressed key is a valid key
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                return containerScroll[STATE.ASSEMBLY].Up();
            case ConsoleKey.DownArrow:
                return containerScroll[STATE.ASSEMBLY].Down();
            case ConsoleKey.Escape:
                ChangeState(STATE.MAIN);
                return true;
        }

        // No valid input
        return false;
    }

    // Handle input in the ERROR screen
    private bool GetInputError(ConsoleKeyInfo key, bool keyMod)
    {
        // Check if the pressed key is a valid key
        switch (key.Key)
        {
            case ConsoleKey.Enter:
                Console.Clear();
                return true;
            case ConsoleKey.Escape:
                running = false;
                return true;
        }

        // No valid input
        return false;
    }

    // Get input from user
    private void GetInput() 
    {
        // Loop until valid input is given
        bool validKey = false;
        while (!validKey)
        {
            // Wait for and get key input from user
            ConsoleKeyInfo key = Console.ReadKey(true);

            // Check if key modifiers was pressed
            bool keyMod = key.Modifiers.HasFlag(ConsoleModifiers.Alt);

            // Call the correct GetInput function for the current application STATE
            switch (appState)
            {
                case STATE.MAIN:
                    switch (mainSubState)
                    {
                        case MAIN_SUB_STATE.DEFAULT:
                            validKey = GetInputMain(key, keyMod);
                            break;
                        case MAIN_SUB_STATE.ADD:
                            validKey = GetInputMainAdd(key, keyMod);
                            break;
                        case MAIN_SUB_STATE.SUBTRACT:
                            validKey = GetInputMainSubtract(key, keyMod);
                            break;
                    }
                    break;
                case STATE.HELP:
                    validKey = GetInputHelp(key, keyMod);
                    break;
                case STATE.SETTINGS:
                    validKey = GetInputSettings(key, keyMod);
                    break;
                case STATE.ASSEMBLY:
                    validKey = GetInputAssembly(key, keyMod);
                    break;
                case STATE.ERROR:
                    validKey = GetInputError(key, keyMod);
                    break;
            }
        }
    }
    
    // Main titlebar
    private void RenderTitlebar()
    {
        Console.WriteLine();
        Console.WriteLine(COLORS.FG_BLACK + COLORS.BG_BRIGHT_CYAN + (MAIN_TITLE.PadLeft((RENDER_WIDTH - MAIN_TITLE.Length) / 2 + MAIN_TITLE.Length, ' ')).PadRight(RENDER_WIDTH, ' ') + COLORS.DEFAULT);
    }

    // Render the MAIN screen
    private void RenderMain()
    {
        // Main title
        RenderTitlebar();

        // Format the display string
        string displayLine = ByteToString(registers[registerIndex]);
       
        // Register selector
        Console.WriteLine();
        string registerLine = " ";
        string registerLineHex = " ";
        for (byte i = 0; i < REGISTER_NUM; i++)
        {
            if (i == registerIndex) { registerLine += COLORS.FG_BLACK + COLORS.BG_BRIGHT_YELLOW; }
            registerLine += "  " + GetRegisterChar(i) + "  ";
            if (i == registerIndex) { registerLine += COLORS.DEFAULT; }
            registerLine += "  ";
            registerLineHex += " 0x" + HexToString(registers[i]) + " ";
            registerLineHex += " ";
        }
        Console.WriteLine(registerLine);
        Console.WriteLine(registerLineHex);
        Console.WriteLine(Lines.seperator[0]);

        // Iterate through the x and y coords of the "pixels" and display the digits from the selected register
        for (int y = 0; y < CHAR_HEIGHT; y++)
        {
            display[y] = " ";
            for (int x = 0; x < CHAR_WIDTH * displayLine.Length; x++)
            {
                // Find the current char from the input string
                char currentChar = displayLine[x / CHAR_WIDTH];

                // Convert the char to a index number for the DIGITS array
                int currentDigit = 0;
                if (Char.IsNumber(currentChar))
                {
                    currentDigit = (int)Char.GetNumericValue(currentChar); 
                }

                // Check if the current bit was changed by last operation
                byte bitToCheck = (byte)(0b10000000 >> (byte)(x / CHAR_WIDTH));
                bool bitChanged = (registers[registerIndex] & bitToCheck) != (registersPrev[registerIndex] & bitToCheck);
                
                // Set the value of the current "pixel"
                display[y] += DIGITS[currentDigit,y,x % CHAR_WIDTH] ? (bitChanged && settings["uiHlChangedBit"].enabled) ? COLORS.FG_BRIGHT_YELLOW + FILLED + COLORS.DEFAULT : FILLED : EMPTY;
            }

            // Render the results of the current row
            Console.WriteLine(display[y]);
        }

        // Decimal values
        Console.WriteLine(Lines.seperator[0]);
        Console.WriteLine(" UNSIGNED VALUE:     " + (registers[registerIndex].ToString()).PadLeft(3,' ') + "        SIGNED VALUE:      " + (((int)((registers[registerIndex] & 0b01111111) - (registers[registerIndex] & 0b10000000))).ToString()).PadLeft(4,' '));
        //Console.WriteLine(" ASCII CHARACTER:  " + (registers[registerIndex] > 32 ? (char)registers[registerIndex] : "-"));

        Console.WriteLine(Lines.seperator[0]);
        
        // Flags
        string carryFlagString = " (C) CARRY FLAG:       " + COLORS.FG_BLACK + (carryFlag ? COLORS.BG_BRIGHT_GREEN + "1" : COLORS.BG_BRIGHT_RED + "0") + COLORS.DEFAULT;
        string zeroFlagString = "        (Z) ZERO FLAG:        " + COLORS.FG_BLACK + (zeroFlag ? COLORS.BG_BRIGHT_GREEN + "1"  : COLORS.BG_BRIGHT_RED + "0") + COLORS.DEFAULT;
        string negativeFlagString = " (N) NEGATIVE FLAG:    " + COLORS.FG_BLACK + (negativeFlag ? COLORS.BG_BRIGHT_GREEN + "1" : COLORS.BG_BRIGHT_RED + "0") + COLORS.DEFAULT;
        string overFlowFlagString = "        (V) OVERFLOW FLAG:    " + COLORS.FG_BLACK + (overflowFlag ? COLORS.BG_BRIGHT_GREEN + "1" : COLORS.BG_BRIGHT_RED + "0") + COLORS.DEFAULT;
        Console.WriteLine(carryFlagString + zeroFlagString);
        Console.WriteLine(negativeFlagString + overFlowFlagString);
        
        Console.WriteLine(Lines.seperator[0]);
       
        // Render message log
        RenderMessages();
        
        Console.WriteLine(Lines.seperator[0]);
        
        // Keyboard shortcuts
        Console.WriteLine(" <UP>              INCREMENT                  (N,Z)");
        Console.WriteLine(" <DOWN>            DECREMENT                  (N,Z)");
        Console.WriteLine((registerIndex == 0 ? "" : COLORS.FG_BRIGHT_BLACK ) + " <LEFT>            ARITHMETIC SHIFT LEFT      (N,Z,C)");
        Console.WriteLine(" <RIGHT>           LOGICAL SHIFT RIGHT        (N,Z,C)");
        Console.WriteLine(" <A+LEFT>          ROTATE LEFT                (N,Z,C)");
        Console.WriteLine(" <A+RIGHT>         ROTATE RIGHT               (N,Z,C)");
        Console.WriteLine(" <PLUS> <0> - <7>  ADD VALUE FROM MEMORY      (N,V,Z,C)");
        Console.WriteLine(" <MINUS> <0> - <7> SUBTRACT VALUE FROM MEMORY (N,V,Z,C)");
        Console.WriteLine((registerIndex == 0 ? "" : COLORS.DEFAULT ) + " <0> - <7>         STORE VALUE TO MEMORY");
        Console.WriteLine(" <A+0> - <A+7>     LOAD VALUE FROM MEMORY     (N,Z)");
        Console.WriteLine(" <A> <X> <Y>       CHANGE ACTIVE REGISTER");
        Console.WriteLine(" <A+A> <A+X> <A+Y> TRANSFER TO REGISTER ");
        Console.WriteLine(" <C>               TOGGLE CARRY FLAG");
        Console.WriteLine(" <V>               CLEAR OVERFLOW FLAG");
        Console.WriteLine(" <L>               ASSEMBLY LOG");
        Console.WriteLine(" <S>               SETTINGS");
        //Console.WriteLine(" <:>               COMMAND MODE (NOT IMPLEMENTED)");
        Console.WriteLine(" <?>               HELP");
        Console.WriteLine(Lines.seperator[0]);
        Console.WriteLine(" PRESS <ESC> TO QUIT PROGRAM");
    }

    // Render the HELP screen
    private void RenderHelp(){
        
        // Main title
        RenderTitlebar();
        Console.WriteLine();

        // Render the HELP container
        containerScroll[STATE.HELP].Render();
    }
    
    // Render the SETTINGS screen
    private void RenderSettings(){
        
        // Main title
        RenderTitlebar();
        Console.WriteLine();
        
        // Render the SETTINGS container
        containerToggle[STATE.SETTINGS].Render();
    }

    // Render the ASSEMBLY screen
    private void RenderAssembly(){
        
        // Main title
        RenderTitlebar();
        Console.WriteLine();

        // Render the ASSEMBLY container
        containerScroll[STATE.ASSEMBLY].Render();
    }

    // Render the ERROR screen
    private void RenderError()
    {
        // Clear the console
        Console.Clear();

        // Show error message
        Console.WriteLine("ERROR!");

        // Show error message if window size is too small
        if (appError["windowSize"])
        {
            Console.WriteLine("WINDOW SIZE IS TOO SMALL!");
            Console.WriteLine("PLEASE RESIZE THE WINDOW");
            Console.WriteLine("AND PRESS <ENTER>");
            Console.WriteLine("OR PRESS <ESC> TO QUIT");
        }
    }

    // Render the result on screen
    private void Render()
    {
        // Clear the console
        Console.SetCursorPosition(0,0);
        for (int i = 0; i < RENDER_HEIGHT; i++){
            Console.WriteLine(Lines.blank);
        }
        Console.SetCursorPosition(0,0);
      
        // Call the correct Render function for the current application STATE
        switch (appState)
        {
            case STATE.MAIN:
                RenderMain();
                break;
            case STATE.HELP:
                RenderHelp();
                break;
            case STATE.SETTINGS:
                RenderSettings();
                break;
            case STATE.ASSEMBLY:
                RenderAssembly();
                break;
            case STATE.ERROR:
                RenderError();
                break;
        }
    }
    
    // Main loop
    public void Run()
    {
        // Clear console and hide cursor
        Console.CursorVisible = false;
        Console.Clear();
        
        while (running == true)
        {
            CheckWindowSize();
            Render();
            GetInput();
        }

        // Exit the program and reset the console
        Console.WriteLine(COLORS.DEFAULT);
        Console.CursorVisible = true;
        Console.Clear();
    }
   
    // Class constructor
    public BinaryCalc()
    {
        Init();
    }
}

