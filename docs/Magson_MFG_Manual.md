นี่คือเอกสาร **Documentation** ฉบับสมบูรณ์ที่รวมวิธีการเชื่อมต่อและข้อมูลทางเทคนิคสำหรับการเขียนโปรแกรม (C Data Types & Sample Code) ตามที่คุณต้องการครับ

คุณสามารถบันทึกข้อความด้านล่างเป็นไฟล์ `.md` (เช่น `Magson_MFG_Manual.md`) เพื่อนำไปใช้งานได้เลยครับ

---

# Magson Digital Fluxgate Magnetometer Integration Guide

**Model:** MFG-1S-LC / MFG-2S-LC
**Document Version:** 1.0 (Based on Manual V2.1A)


---

## 1. การเชื่อมต่อและดึงข้อมูล (Connection Methods)

เครื่อง Magnetometer รองรับการดึงข้อมูลผ่าน **Ethernet (LAN)** โดยไม่ต้องใช้อุปกรณ์แปลงสัญญาณเพิ่มเติม

### 1.1 การตั้งค่าเครือข่าย (Network Setup)

* 
**IP Address:** ดูได้จากหน้าจอทัชสกรีน เมนู `Config` > `Network` 


* 
**Port สำหรับเขียนโปรแกรม (Socket):** `12345` 



### 1.2 ช่องทางการเข้าถึงข้อมูล

| วิธีการ | จุดเด่น | การใช้งาน |
| --- | --- | --- |
| **Web Interface** | ดูค่าสดและกราฟผ่าน Browser (อัปเดตทุก 2 วิ) | พิมพ์ `http://<IP_ADDRESS>` ใน Browser 

 |
| **FTP** | ดาวน์โหลดไฟล์ Log ย้อนหลัง (.A, .B) | ใช้โปรแกรม FTP (User: `ftpuser`, Pass: `fluxgate`) 

 |
| **TCP Socket** | รับข้อมูลดิบ Real-time (สูงสุด 100/200Hz) | เชื่อมต่อ TCP Port `12345` (รองรับสูงสุด 5 Clients) 

 |

---

## 2. โครงสร้างข้อมูลภาษา C (C Data Structures & Types)

เมื่อเชื่อมต่อผ่าน **TCP Socket (Port 12345)** ข้อมูลจะถูกส่งมาในรูปแบบ Binary Structure (Little-Endian) โดยใช้โครงสร้างดังนี้:

### 2.1 Main Data Structure

โครงสร้างหลักที่ใช้รับส่งข้อมูลทุกประเภท :

```c
struct mag_data_struct {
    long DataType;      // ระบุประเภทของข้อมูล (ดูหัวข้อ 2.2)
    long l[3];          // Array เก็บข้อมูล Integer (เช่น Timestamp, Counters)
    float f[14];        // Array เก็บข้อมูล Float (เช่น Magnetic Field, Temp)
};

```

*หมายเหตุ: รุ่น 1 Sensor ใช้ `f[11]`, รุ่น 2 Sensors ใช้ `f[14]` (แนะนำให้จองเผื่อเป็น 14)*

### 2.2 Data Type Definitions (Constants)

ค่าคงที่ระบุประเภทของข้อมูลในตัวแปร `DataType` :

```c
#define TYPE_DAT 1      // Measurement Values (ข้อมูลการวัด)
#define TYPE_REP 2      // Command Reply (ตอบกลับคำสั่ง)
#define TYPE_POS 3      // GPS Position
#define TYPE_SDS 4      // SD Card Status
#define TYPE_LOG 5      // Log Messages
#define TYPE_CCN 6      // Capture Counter
#define TYPE_HTS 7      // Heater Status (Optional)

```

### 2.3 Array Index Mapping

ตำแหน่งของข้อมูลใน Array `l[]` และ `f[]` ขึ้นอยู่กับ `DataType` ดังนี้:

#### A. 

TYPE_DAT (Measurement Data) 

*ส่งมาต่อเนื่องตาม Sampling Rate ที่ตั้งไว้*

| Index Macro | Value (Index) | Description |
| --- | --- | --- |
| `TYPE_DAT_L_TIMESTAMP` | `l[0]` | Timestamp (seconds since 1.1.1970) |
| `TYPE_DAT_L_STATUS` | `l[1]` | Status Word |
| `TYPE_DAT_F_TS1` | `f[0]` | Sensor 1 Temperature (°C) |
| `TYPE_DAT_F_TE` | `f[1]` | Electronics Temperature (°C) |
| `TYPE_DAT_F_TS2` | `f[7]` | Sensor 2 Temperature (°C) *(Dual only)* |
| `TYPE_DAT_F_BX1` | `f[8]` | Magnetic Field X - Sensor 1 (nT) |
| `TYPE_DAT_F_BY1` | `f[9]` | Magnetic Field Y - Sensor 1 (nT) |
| `TYPE_DAT_F_BZ1` | `f[10]` | Magnetic Field Z - Sensor 1 (nT) |
| `TYPE_DAT_F_BX2` | `f[11]` | Magnetic Field X - Sensor 2 (nT) *(Dual only)* |
| `TYPE_DAT_F_BY2` | `f[12]` | Magnetic Field Y - Sensor 2 (nT) *(Dual only)* |
| `TYPE_DAT_F_BZ2` | `f[13]` | Magnetic Field Z - Sensor 2 (nT) *(Dual only)* |

#### B. 

TYPE_POS (GPS Data) 

| Index Macro | Value (Index) | Description |
| --- | --- | --- |
| `TYPE_POS_L_TIMESTAMP` | `l[0]` | Timestamp |
| `TYPE_POS_F_LAT` | `f[0]` | Latitude (Degrees) |
| `TYPE_POS_F_LON` | `f[1]` | Longitude (Degrees) |

#### C. 

TYPE_SDS (SD Card Info) 

| Index Macro | Value (Index) | Description |
| --- | --- | --- |
| `TYPE_SDS_L_TIMESTAMP` | `l[0]` | Timestamp |
| `TYPE_SDS_F_CARDSIZE` | `f[0]` | Total Disk Space (MB) |
| `TYPE_SDS_F_CARDUSAGE` | `f[1]` | Used Disk Space (MB) |

#### D. 

TYPE_HTS (Heater Status) 

| Index Macro | Value (Index) | Description |
| --- | --- | --- |
| `TYPE_HTS_L_TIMESTAMP` | `l[0]` | Timestamp |
| `TYPE_HTS_L_ENABLE` | `l[1]` | Enable Flags (Bit 0: Sensor, Bit 1: Elec) |
| `TYPE_HTS_F_SETPOINTE` | `f[0]` | Electronics Setpoint (°C) |
| `TYPE_HTS_F_SETPOINTS` | `f[1]` | Sensor Setpoint (°C) |
| `TYPE_HTS_F_PIDOUTE` | `f[2]` | Electronics PID Output (%) |
| `TYPE_HTS_F_PIDOUTS1` | `f[3]` | Sensor 1 PID Output (%) |
| `TYPE_HTS_F_PIDOUTS2` | `f[4]` | Sensor 2 PID Output (%) |

---

## 3. ตัวอย่างโค้ดภาษา C (Sample Code)

โค้ดด้านล่างเป็นการเชื่อมต่อผ่าน Socket เพื่อดึงข้อมูลและแสดงผล (ปรับปรุงจากคู่มือ Appendix 11.3) 

```c
#include <stdio.h>
#include <winsock.h> // หรือ <winsock2.h> บน Windows
#include <conio.h>

/* --- DEFINITIONS --- */
#define TYPE_DAT 1
#define TYPE_REP 2
#define TYPE_POS 3
#define TYPE_SDS 4
#define TYPE_LOG 5
#define TYPE_CCN 6
#define TYPE_HTS 7

// Index Mapping
#define TYPE_DAT_L_TIMESTAMP 0
#define TYPE_DAT_L_STATUS 1
#define TYPE_DAT_F_TS1 0
#define TYPE_DAT_F_TE 1
#define TYPE_DAT_F_TS2 7
#define TYPE_DAT_F_BX1 8
#define TYPE_DAT_F_BY1 9
#define TYPE_DAT_F_BZ1 10
#define TYPE_DAT_F_BX2 11
#define TYPE_DAT_F_BY2 12
#define TYPE_DAT_F_BZ2 13

#define TYPE_POS_L_TIMESTAMP 0
#define TYPE_POS_F_LAT 0
#define TYPE_POS_F_LON 1

#define TYPE_SDS_L_TIMESTAMP 0
#define TYPE_SDS_F_CARDSIZE 0
#define TYPE_SDS_F_CARDUSAGE 1

#define ESC 27
#define LF 10
#define CR 13
#define MAXCMDLENGTH 64

/* --- DATA STRUCTURE --- */
struct mag_data_struct {
    long DataType;
    long l[3];
    float f[14];
};

/* --- MAIN PROGRAM --- */
int main(int argc, char *argv[])
{
    int sock;
    struct sockaddr_in ServAddr;
    unsigned short ServPort;
    char *servIP;
    int bytesread;
    WSADATA wsaData;
    char key=0;
    char command[MAXCMDLENGTH+2];
    int EXIT=0;
    int charcnt=0;
    struct mag_data_struct magdat;

    // ตรวจสอบ Arguments
    if (argc != 3) {
        fprintf(stderr, "Usage: %s <Server IP> <Port>\n", argv[0]);
        return -1;
    }

    servIP = argv[1];
    ServPort = atoi(argv[2]);

    // Initialize Winsock
    if (WSAStartup(MAKEWORD(2, 0), &wsaData) != 0) {
        fprintf(stderr, "WSAStartup() failed");
        return -2;
    }

    // สร้าง Socket (TCP)
    if ((sock = socket(PF_INET, SOCK_STREAM, IPPROTO_TCP)) < 0) {
        fprintf(stderr, "socket() failed");
        return -3;
    }

    memset(&ServAddr, 0, sizeof(ServAddr));
    ServAddr.sin_family = AF_INET;
    ServAddr.sin_addr.s_addr = inet_addr(servIP);
    ServAddr.sin_port = htons(ServPort);

    // เชื่อมต่อ Server
    if (connect(sock, (struct sockaddr *) &ServAddr, sizeof(ServAddr)) < 0) {
        fprintf(stderr, "connect() failed");
        return -4;
    }

    printf("Connected to %s:%d. Press ESC to exit.\n", servIP, ServPort);

    // ลูปหลัก: รับข้อมูลและส่งคำสั่ง
    do {
        // 1. ตรวจสอบการกดปุ่ม (เพื่อส่งคำสั่ง)
        while (kbhit()) {
            key = _getch();
            switch (key) {
                case ESC: 
                    EXIT=1; 
                    break;
                case CR:
                case LF:
                    command[charcnt] = LF;
                    command[charcnt+1] = '\0';
                    printf("\nSending command: %s", command);
                    // ส่งคำสั่งไปที่เครื่อง
                    if (send(sock, command, strlen(command), 0) != (int)strlen(command)) {
                        fprintf(stderr, "send() failed");
                    }
                    charcnt = 0;
                    break;
                default:
                    if (charcnt < MAXCMDLENGTH) {
                        command[charcnt] = key;
                        charcnt++;
                        printf("%c", key); // Echo ตัวอักษร
                    }
                    break;
            }
        }

        // 2. รับข้อมูลจาก Socket
        bytesread = recv(sock, (char*)&magdat, sizeof(magdat), 0);
        
        if (bytesread == sizeof(magdat)) {
            // แยกประเภทข้อมูลแล้วแสดงผล
            switch (magdat.DataType) {
                case TYPE_DAT:
                    printf("Time:%02ld:%02ld:%02ld | S1(%.2f, %.2f, %.2f) | S2(%.2f, %.2f, %.2f)\n",
                        ((magdat.l[TYPE_DAT_L_TIMESTAMP]/3600)%24),
                        ((magdat.l[TYPE_DAT_L_TIMESTAMP]/60)%60),
                        (magdat.l[TYPE_DAT_L_TIMESTAMP]%60),
                        magdat.f[TYPE_DAT_F_BX1], magdat.f[TYPE_DAT_F_BY1], magdat.f[TYPE_DAT_F_BZ1],
                        magdat.f[TYPE_DAT_F_BX2], magdat.f[TYPE_DAT_F_BY2], magdat.f[TYPE_DAT_F_BZ2]
                    );
                    break;
                
                case TYPE_POS:
                    printf("[GPS] Lat: %.5f, Lon: %.5f\n", 
                        magdat.f[TYPE_POS_F_LAT], magdat.f[TYPE_POS_F_LON]);
                    break;

                case TYPE_SDS:
                    printf("[SD Card] Used: %.2f MB / Total: %.2f MB\n", 
                        magdat.f[TYPE_SDS_F_CARDUSAGE], magdat.f[TYPE_SDS_F_CARDSIZE]);
                    break;

                default:
                    // printf("Received DataType: %ld\n", magdat.DataType);
                    break;
            }
        }
    } while ((bytesread > 0) && (!EXIT));

    closesocket(sock);
    WSACleanup();
    return 0;
}

```