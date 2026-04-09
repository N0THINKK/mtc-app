# 🚀 Deployment Guide for MTC App
This guide explains how to deploy the Maintenance App to client computers (Technicians, Operators, Group Leaders) and set up a shared database on your network.
## 1. Concept 🌍
-   **Server PC:** ONE computer (yours?) runs the Database (MariaDB). It must stay ON.
-   **Client PCs:** Other computers (Technicians/GLs) run the App EXE and connect to your Server via Wi-Fi/LAN.
## 2. Prepare the Server 🖥️
*(Do this on the computer that has MariaDB installed)*
### Step A: Find your IP Address
1.  Press `Windows + R`, type `cmd`, press Enter.
2.  Type `ipconfig` and press Enter.
3.  Look for **IPv4 Address**. It usually looks like `192.168.1.xxx`.
    *   *Example:* `192.168.1.105` (Remember this!)
### Step B: Allow Remote Connections (Database)
You need to create a user that is allowed to connect from other computers.
1.  Open HeidiSQL or your database tool.
2.  Run this SQL Query:
    ```sql
    -- Create a user 'mtc_user' that can connect from ANY IP ('%')
    CREATE USER 'mtc_user'@'%' IDENTIFIED BY 'mtc_password';
    
    -- Give them permission to use the database 'db_maintenance'
    GRANT ALL PRIVILEGES ON db_maintenance.* TO 'mtc_user'@'%';
    
    -- Save changes
    FLUSH PRIVILEGES;
    ```
    *(Change 'mtc_password' to a secure password)*
### Step C: Open Firewall Port (Important!)
Windows blocks connections by default. You must open Port 3306.
1.  Press Start, search for **"Windows Defender Firewall with Advanced Security"**.
2.  Click **Inbound Rules** (Left sidebar).
3.  Click **New Rule...** (Right sidebar).
4.  Select **Port** -> Next.
5.  Select **TCP** and enter **3306** in "Specific local ports".
6.  Select **Allow the connection** -> Next -> Next.
7.  Name it: `MariaDB SQL` -> Finish.
## 3. Build the App 📦
1.  Open the Project in Visual Studio.
2.  In the top toolbar, change `Debug` to **`Release`**.
3.  Go to **Build** menu -> **Rebuild Solution**.
4.  Once finished, right-click the project in Solution Explorer -> **Open Folder in File Explorer**.
5.  Navigate to `bin\Release`.
    *   *Note: If you see `net48`, go inside it.*
6.  You should see `mtc-app.exe` and `appsettings.json`.
## 4. Deploy to Clients 🚀
1.  **Copy** the entire contents of the `Release` folder to a Flashdisk or Shared Folder.
2.  **Paste** it onto the Client PC (e.g., in `C:\MtcApp\`).
3.  **Edit Configuration:**
    *   Open `appsettings.json` on the Client PC with **Notepad**.
    *   Find the `DefaultConnection` line.
    *   Change `localhost` to your **Server IP**.
    *   Change User/Password to the ones you created in Step 2B.
    
    *Example:*
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=192.168.1.105;Database=db_maintenance;User=mtc_user;Password=mtc_password;"
    }
    ```
4.  **Save** and Close.
## 5. Run & Test ✅
1.  Double-click `mtc-app.exe` on the Client PC.
2.  It should open and show data from your Server!
3.  **Test Offline Mode:**
    *   While connected, verify data loads (Cache Warmed).
    *   Disconnect Wi-Fi on Client.
    *   Restart App -> It should still work! (Using local cache).