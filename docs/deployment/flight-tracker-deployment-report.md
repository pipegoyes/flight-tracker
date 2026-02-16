# ✈️ Flight Tracker - Cloud Deployment Report (Updated with Free Tiers)

**Generated:** 2026-02-10 (Updated)  
**Application:** .NET 8.0 Blazor Server with Docker  
**Region Preference:** Germany / Europe  
**Expected Traffic:** Low (personal use + background jobs)  

---

## 🆓 FREE TIER ANALYSIS (First 12 Months)

### 🎁 AWS Free Tier (12 Months)

**What's Included:**
- **EC2:** 750 hours/month of **t2.micro** (1 vCPU, 1 GB RAM) - **FREE**
- **EBS:** 30 GB General Purpose SSD storage - **FREE**
- **Data Transfer:** 15 GB/month outbound - **FREE**
- **Snapshots:** First 1 GB - **FREE**

**💰 First Year Cost: ~$0-2/month** (only if you exceed limits)

**✅ Covers Flight Tracker:** YES! Fully free for 12 months
- App uses <1 GB RAM ✓
- Single instance = 730 hours/month < 750 hours ✓
- Storage ~10 GB < 30 GB ✓
- Traffic <1 GB/month < 15 GB ✓

---

### 🎁 Azure Free Tier (12 Months)

**What's Included:**
- **App Service:** 750 hours/month of **B1S Linux** (1 core, 1.75 GB RAM) - **FREE**
- **Virtual Machines:** 750 hours/month of **B1S** (1 vCPU, 1 GB RAM) - **FREE**
- **Managed Disks:** 2 × 64 GB P6 SSD - **FREE**
- **Data Transfer:** 15 GB/month outbound - **FREE**
- **Azure Database:** 250 GB (not needed for SQLite)

**💰 First Year Cost: ~$0/month** (fully covered)

**✅ Covers Flight Tracker:** YES! Fully free for 12 months
- Can use App Service B1S (best option) ✓
- Or VM B1S with full control ✓
- Storage well within limits ✓
- Traffic well within limits ✓

---

## 🏆 NEW RECOMMENDATION: USE FREE TIERS!

### 🥇 **Azure App Service (Free Tier) - $0/month for 12 months** ⭐⭐⭐

**Why Azure wins for free tier:**
- **App Service B1S:** Purpose-built for web apps
- **Managed platform:** No OS updates, automatic scaling
- **Docker support:** Deploy directly from GitHub
- **SSL included:** Free HTTPS certificates
- **Application Insights:** Free monitoring (5 GB/month)
- **Better than VM:** No server management

**After 12 months:** $12-13/month (still cheaper than AWS Lightsail)

---

### 🥈 **AWS EC2 t2.micro (Free Tier) - $0/month for 12 months** ⭐⭐

**Why AWS EC2 as alternative:**
- **Full control:** Can install anything
- **Linux:** Ubuntu/Amazon Linux included
- **EBS storage:** 30 GB included
- **Static IP:** Free when attached
- **Familiar:** Similar to current t3.medium setup

**After 12 months:** ~$8-10/month (t2.micro pricing)

---

## 📋 Application Requirements

| Requirement | Specification |
|-------------|---------------|
| **Runtime** | .NET 8.0 (Docker container) |
| **Database** | SQLite (file-based, 1-10 MB) |
| **Memory** | 512 MB - 1 GB RAM |
| **CPU** | 1 vCPU (0.5 vCPU may suffice) |
| **Storage** | 10 GB (logs, DB, container) |
| **Network** | Minimal traffic (<1 GB/month) |
| **Background Jobs** | 2x daily (8 AM, 8 PM CET) |
| **Availability** | Standard (no HA required) |

---

## ☁️ AWS Deployment Options

### Option 1: AWS EC2 t2.micro (Free Tier) ⭐ **FREE FOR 12 MONTHS**

**Description:** Traditional virtual machine, fully free for first year.

#### Components:
- **EC2 Instance** (t2.micro, 1 vCPU, 1 GB RAM)
- **EBS Volume** (30 GB gp2/gp3)
- **Elastic IP** (free when attached)

#### Pricing:
| Period | Monthly Cost |
|--------|-------------|
| **Months 1-12** | **$0.00** (Free Tier) |
| **Months 13+** | **$8.50/month** |

#### Free Tier Coverage:
- ✅ 750 hours/month EC2 (covers 24/7 operation)
- ✅ 30 GB EBS storage
- ✅ 15 GB data transfer/month
- ✅ 1 GB snapshots

#### Pros:
✅ **FREE for 12 months!**  
✅ Full control over environment  
✅ Can run Docker natively  
✅ Familiar Linux environment  
✅ All regions available  

#### Cons:
❌ Manual OS management  
❌ Security patches required  
❌ No built-in monitoring (CloudWatch costs extra)  

---

### Option 2: AWS Lightsail

**Description:** Simplified VPS with predictable pricing, perfect for small applications.

#### Components:
- **Lightsail Instance** (1 GB RAM, 1 vCPU, 40 GB SSD)
- **Container service** OR **Virtual server** (both supported)
- **Static IP** (included)
- **Data transfer** (1 TB included)

#### Pricing (Frankfurt region):
| Component | Monthly Cost |
|-----------|-------------|
| Instance (1 GB RAM) | **$5.00/month** |
| Data transfer | Included (1 TB) |
| Snapshots (10 GB) | $0.50/month |
| **TOTAL** | **~$5.50/month** |

**⚠️ Note:** Lightsail NOT eligible for free tier

#### Pros:
✅ Simplest AWS option  
✅ Predictable fixed pricing  
✅ 1 TB data transfer included  
✅ Easy to manage via console  
✅ Can run Docker directly  

#### Cons:
❌ No free tier  
❌ Less flexible than EC2  
❌ Basic monitoring  

---

### Option 3: AWS ECS Fargate

**Description:** Serverless container service, pay only for compute time.

#### Components:
- **ECS Fargate Task** (0.5 vCPU, 1 GB RAM)
- **Application Load Balancer** (optional, adds $16/month)
- **EFS** for persistent storage (SQLite database)
- **CloudWatch** for logging

#### Pricing (Frankfurt region):
| Component | Calculation | Monthly Cost |
|-----------|-------------|-------------|
| Fargate vCPU (0.5) | $0.04656/hour × 730h | $34.00/month |
| Fargate RAM (1 GB) | $0.00511/GB/hour × 730h | $3.73/month |
| EFS storage (1 GB) | $0.35/GB | $0.35/month |
| ALB (if needed) | $0.0252/hour × 730h + LCU | $18.40/month |
| Data transfer | First 100 GB free | $0.00/month |
| **TOTAL (without ALB)** | | **~$38/month** |
| **TOTAL (with ALB)** | | **~$56/month** |

**⚠️ Note:** Fargate NOT eligible for free tier

#### Pros:
✅ Serverless (no server management)  
✅ Auto-scaling capabilities  
✅ Integrated monitoring  

#### Cons:
❌ More expensive for 24/7 workload  
❌ Complex setup  
❌ Requires EFS for SQLite (adds latency)  
❌ No free tier  

---

## ☁️ Azure Deployment Options

### Option 1: Azure App Service B1S (Free Tier) ⭐ **FREE FOR 12 MONTHS**

**Description:** Managed PaaS for web apps with Docker support, FREE for first year!

#### Components:
- **App Service Plan** (B1S Basic - 1 vCPU, 1.75 GB RAM)
- **App Service** (Linux with Docker)
- **Application Insights** (monitoring, free tier)

#### Pricing (West Europe region):
| Period | Monthly Cost |
|--------|-------------|
| **Months 1-12** | **$0.00** (Free Tier) ✨ |
| **Months 13+** | **€11.97/month** (~$13/month) |

#### Free Tier Coverage:
- ✅ 750 hours/month App Service B1S (covers 24/7)
- ✅ 64 GB managed disk (×2)
- ✅ 15 GB data transfer/month
- ✅ SSL certificate included
- ✅ Application Insights (5 GB free tier)

#### Pros:
✅ **FREE for 12 months!** 🎉  
✅ Managed platform (no OS updates)  
✅ Easy Docker deployment from GitHub  
✅ Built-in monitoring (App Insights)  
✅ Custom domains + SSL included  
✅ Easy scaling  
✅ CI/CD built-in  

#### Cons:
❌ Costs start after 12 months ($13/month)  
❌ Less control than VM  

---

### Option 2: Azure Virtual Machine B1S (Free Tier) ⭐ **FREE FOR 12 MONTHS**

**Description:** Traditional VM with full control, FREE for first year.

#### Components:
- **VM (B1S)** (1 vCPU, 1 GB RAM)
- **Managed Disk** (Standard SSD, 64 GB)
- **Public IP** (basic)

#### Pricing (West Europe region):
| Period | Monthly Cost |
|--------|-------------|
| **Months 1-12** | **$0.00** (Free Tier) |
| **Months 13+** | **~$10/month** |

#### Free Tier Coverage:
- ✅ 750 hours/month VM B1S
- ✅ 64 GB × 2 managed disks
- ✅ 15 GB data transfer

#### Pros:
✅ **FREE for 12 months!**  
✅ Full control  
✅ Can install anything  
✅ Similar to EC2 experience  

#### Cons:
❌ Manual management  
❌ Security patches required  
❌ Costs ~$10/month after free tier  

---

### Option 3: Azure Container Instances (ACI)

**Description:** Serverless containers, pay-per-second billing.

#### Components:
- **Container Instance** (1 vCPU, 1.5 GB RAM)
- **Azure Files** for persistent storage

#### Pricing (West Europe region):
| Component | Calculation | Monthly Cost |
|-----------|-------------|-------------|
| Container (1 vCPU) | $0.0000144/second × 2,628,000s | $37.84/month |
| Memory (1.5 GB) | $0.0000016/GB/s × 2,628,000s | $6.31/month |
| Azure Files (1 GB) | $0.18/GB | $0.18/month |
| **TOTAL** | | **~$44/month** |

**⚠️ Note:** ACI NOT eligible for free tier

#### Pros:
✅ Pay only for running time  
✅ Fast deployment  
✅ No VM management  

#### Cons:
❌ More expensive for 24/7 workload  
❌ No built-in load balancing  
❌ Persistence requires Azure Files  
❌ No free tier  

---

## 📊 Updated Cost Comparison (With Free Tiers)

### Year 1 Costs (Months 1-12)

| Rank | Option | Cloud | Months 1-12 | Months 13+ | Best For |
|------|--------|-------|-------------|------------|----------|
| 🥇 1 | **Azure App Service B1S** | Azure | **$0** 🎉 | $13/month | Managed PaaS, zero ops |
| 🥇 1 | **AWS EC2 t2.micro** | AWS | **$0** 🎉 | $8.50/month | Full control, traditional |
| 🥇 1 | **Azure VM B1S** | Azure | **$0** 🎉 | $10/month | Full control on Azure |
| 4 | **AWS Lightsail** | AWS | **$5.50** | $5.50/month | Simple, predictable |
| 5 | EC2 t3.micro (no free) | AWS | **$10** | $10/month | Current AWS region |
| 6 | Fargate (no ALB) | AWS | **$38** | $38/month | Serverless containers |
| 7 | ACI | Azure | **$44** | $44/month | Azure serverless |
| 8 | Fargate (with ALB) | AWS | **$56** | $56/month | Production serverless |

---

### 3-Year Total Cost Comparison

| Option | Year 1 | Year 2 | Year 3 | **3-Year Total** |
|--------|--------|--------|--------|------------------|
| **Azure App Service (free tier)** | $0 | $156 | $156 | **$312** 🏆 |
| **AWS EC2 t2.micro (free tier)** | $0 | $102 | $102 | **$204** 🏆 |
| **Azure VM B1S (free tier)** | $0 | $120 | $120 | **$240** 🏆 |
| AWS Lightsail | $66 | $66 | $66 | **$198** |
| AWS EC2 t3.micro | $120 | $120 | $120 | **$360** |
| Current EC2 t3.medium | $420 | $420 | $420 | **$1,260** ❌ |

**Savings vs current setup (3 years):**
- Azure App Service: **$948 saved** 💰
- AWS EC2 t2.micro: **$1,056 saved** 💰💰
- Azure VM B1S: **$1,020 saved** 💰

---

## 🏆 UPDATED RECOMMENDATIONS

### 🥇 #1 Choice: **Azure App Service B1S (Free Tier)** ⭐⭐⭐

**Total Cost:**
- **Year 1:** $0 (FREE) 🎉
- **Years 2-3:** $13/month
- **3-year total:** $312

**Why Azure App Service wins:**
1. **FREE for 12 months** with full features
2. **Zero server management** - no OS patches, no Docker management
3. **Deploy from GitHub** - push to main → auto-deploy
4. **SSL included** - free HTTPS certificates
5. **Built-in monitoring** - Application Insights (free tier)
6. **Best DevOps experience** - CI/CD, deployment slots
7. **West Europe region** - GDPR compliant, close to Germany

**Migration Steps:**
1. Sign up for Azure free account
2. Create App Service (select free B1S tier)
3. Connect to your GitHub repo
4. Configure deployment settings
5. Set environment variables (Sentry DSN)
6. Deploy and test
7. **$0 for first year!**

**After 12 months:** Decide if you want to keep it ($13/month) or migrate elsewhere

---

### 🥈 #2 Choice: **AWS EC2 t2.micro (Free Tier)** ⭐⭐

**Total Cost:**
- **Year 1:** $0 (FREE) 🎉
- **Years 2-3:** $8.50/month
- **3-year total:** $204

**Why AWS EC2 is great alternative:**
1. **FREE for 12 months**
2. **Full control** - install anything you want
3. **Familiar** - similar to your current EC2 setup
4. **Simple migration** - just copy Docker setup
5. **Cheapest after free tier** - only $8.50/month ongoing
6. **Frankfurt region** available
7. **30 GB storage** included in free tier

**Migration Steps:**
1. Launch t2.micro instance (Frankfurt)
2. Select "Free tier eligible" AMI (Amazon Linux 2 or Ubuntu)
3. Configure security group (ports 22, 80, 443, 8080)
4. Install Docker
5. Copy flight-tracker container from current EC2
6. Configure SQLite persistent volume
7. Test thoroughly
8. **$0 for first year!**

---

### 🥉 #3 Choice: **Azure VM B1S (Free Tier)** ⭐

**Total Cost:**
- **Year 1:** $0 (FREE)
- **Years 2-3:** $10/month
- **3-year total:** $240

**Why Azure VM is third choice:**
1. **FREE for 12 months**
2. **Full control** like EC2
3. **Azure ecosystem** if you prefer Microsoft
4. **Good middle ground** between App Service and EC2

**When to choose this:**
- You want Azure but need full VM control
- App Service feels too restrictive
- You're comfortable with server management

---

## 💡 Special Consideration: Migrate TWICE Strategy

**Smart approach to maximize savings:**

### Phase 1 (Year 1): Use Azure App Service (FREE)
- Deploy to Azure App Service B1S
- Save $420 compared to current EC2
- Enjoy zero-ops managed platform
- Learn Azure ecosystem

### Phase 2 (Year 2+): Evaluate options
When free tier ends after 12 months, choose:

**Option A:** Stay on Azure App Service ($13/month)  
- If you love the managed experience
- Total 3-year cost: $312

**Option B:** Migrate to AWS Lightsail ($5.50/month)  
- To minimize long-term costs
- Total 3-year cost: $66 + $132 = **$198**
- **Best of both worlds!** 🎉

**Option C:** Migrate to AWS EC2 t2.micro (no longer free)  
- For full control at low cost ($8.50/month)
- Total 3-year cost: $204

---

## 🎯 Final Recommendation

### START WITH: **Azure App Service B1S (Free Tier)** 🏆

**Benefits:**
- ✅ **$0 for first year** = $420 saved immediately
- ✅ Zero server management
- ✅ Built-in CI/CD from GitHub
- ✅ SSL certificates included
- ✅ Application Insights monitoring
- ✅ Learn Azure platform (valuable skill)
- ✅ Can always migrate later

**After 12 months, reassess:**
- If you love Azure → Stay at $13/month
- If you want cheapest → Migrate to Lightsail ($5.50/month)
- If you want AWS → Move to EC2 t2.micro ($8.50/month)

**Total 3-year savings vs current setup:** 
- Best case: $420 (year 1) + $354 (years 2-3 on Lightsail) = **$774 saved** 💰💰

---

## 📋 Free Tier Gotchas & Important Notes

### AWS Free Tier:
⚠️ **Expires exactly 12 months from signup date**  
⚠️ Must use **t2.micro** (not t3.micro) for free tier  
⚠️ Only **one instance** at a time qualifies  
⚠️ 750 hours/month = covers ONE 24/7 instance  
⚠️ Exceeding 30 GB storage triggers charges  
⚠️ Free tier is per **AWS account** (one-time, can't reset)

**Check eligibility:** https://console.aws.amazon.com/billing/home#/freetier

### Azure Free Tier:
⚠️ **12 months from account creation**  
⚠️ Must select **B1S** tier specifically (not B1)  
⚠️ App Service OR VM (both 750 hours = can run one 24/7)  
⚠️ Some services have "always free" tiers (Functions, etc.)  
⚠️ Credit card required but won't be charged within limits  
⚠️ Free tier is per **subscription**

**Check eligibility:** https://portal.azure.com → Subscriptions → Free services

---

## 🚀 Next Steps

### Immediate Action (Today):

1. **Check your Azure free tier status:**
   - Log into Azure Portal
   - Go to Subscriptions
   - Check "Free services" remaining time
   - If you have months left → **GO FOR IT!**

2. **Check your AWS free tier status:**
   - Log into AWS Console
   - Go to Billing → Free Tier
   - Check if you're still within 12-month window
   - If yes → **EC2 t2.micro is free!**

3. **Choose based on what's available:**
   - **Both free?** → Start with Azure App Service (easiest)
   - **Only Azure free?** → Use Azure App Service
   - **Only AWS free?** → Use EC2 t2.micro
   - **Neither free?** → Use AWS Lightsail ($5.50/month)

---

## 📞 Ready to Deploy?

I can help you:
1. Check your free tier eligibility
2. Set up Azure App Service (fastest, zero-ops)
3. Set up AWS EC2 t2.micro (full control)
4. Migrate Docker containers
5. Configure SSL/DNS
6. Set up monitoring

**Time estimate:** 30-60 minutes for complete migration

Let me know which option you prefer! 🐸
