# ✈️ Flight Tracker - Cloud Deployment Report

**Generated:** 2026-02-10  
**Application:** .NET 8.0 Blazor Server with Docker  
**Region Preference:** Germany / Europe  
**Expected Traffic:** Low (personal use + background jobs)  

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

### Option 1: AWS Lightsail ⭐ **RECOMMENDED**

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

#### Pros:
✅ Simplest AWS option  
✅ Predictable fixed pricing  
✅ 1 TB data transfer included  
✅ Easy to manage via console  
✅ Can run Docker directly  

#### Cons:
❌ Less flexible than EC2  
❌ Limited scaling options  
❌ Basic monitoring  

---

### Option 2: AWS ECS Fargate

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

#### Pros:
✅ Serverless (no server management)  
✅ Auto-scaling capabilities  
✅ Integrated monitoring  

#### Cons:
❌ More expensive for 24/7 workload  
❌ Complex setup  
❌ Requires EFS for SQLite (adds latency)  

---

### Option 3: AWS App Runner

**Description:** Fully managed container service optimized for web apps.

#### Components:
- **App Runner Service** (1 vCPU, 2 GB RAM)
- **Provisioned instance** (always-on)
- **Automatic HTTPS** (included)

#### Pricing (Frankfurt region):
| Component | Calculation | Monthly Cost |
|-----------|-------------|-------------|
| Compute (provisioned) | $0.007/GB-hour × 2 GB × 730h | $10.22/month |
| vCPU | $0.081/vCPU-hour × 1 × 730h | $59.13/month |
| Build | Included | $0.00/month |
| **TOTAL** | | **~$69/month** |

#### Pros:
✅ Zero infrastructure management  
✅ Automatic HTTPS  
✅ Integrated CI/CD  

#### Cons:
❌ Most expensive option  
❌ SQLite persistence challenging  
❌ Limited to HTTP workloads  

---

### Option 4: AWS EC2 (Current Setup)

**Description:** Traditional virtual machine with full control.

#### Components:
- **EC2 Instance** (t3.medium, 2 vCPU, 4 GB RAM)
- **EBS Volume** (20 GB gp3)
- **Elastic IP** (free when attached)

#### Pricing (Frankfurt region):
| Component | Calculation | Monthly Cost |
|-----------|-------------|-------------|
| EC2 t3.medium | $0.0456/hour × 730h | $33.29/month |
| EBS gp3 (20 GB) | $0.088/GB | $1.76/month |
| Data transfer | First 100 GB free | $0.00/month |
| **TOTAL** | | **~$35/month** |

**Note:** Current setup is over-provisioned (t3.medium). Could downgrade to **t3.micro** ($0.0114/hour = **$8.32/month**) for ~**$10/month total**.

#### Pros:
✅ Full control and flexibility  
✅ Can downgrade to t3.micro  
✅ Familiar environment  

#### Cons:
❌ Manual server management  
❌ Security updates required  
❌ No automatic scaling  

---

## ☁️ Azure Deployment Options

### Option 1: Azure App Service (Linux) ⭐ **RECOMMENDED**

**Description:** Managed PaaS for web apps with Docker support.

#### Components:
- **App Service Plan** (B1 Basic - 1 vCPU, 1.75 GB RAM)
- **App Service** (Linux with Docker)
- **Application Insights** (monitoring, free tier)

#### Pricing (West Europe region):
| Component | Monthly Cost |
|-----------|-------------|
| B1 Basic Plan | **€11.97/month** (~$13/month) |
| Storage (10 GB) | Included |
| SSL certificate | Included |
| Custom domain | Included |
| App Insights (5 GB) | Free |
| **TOTAL** | **~$13/month** |

#### Pros:
✅ Managed platform (no OS updates)  
✅ Easy Docker deployment  
✅ Built-in monitoring  
✅ Custom domains + SSL included  
✅ Easy scaling  

#### Cons:
❌ Slightly more expensive than Lightsail  
❌ Windows tax (though using Linux)  

---

### Option 2: Azure Container Instances (ACI)

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

#### Pros:
✅ Pay only for running time  
✅ Fast deployment  
✅ No VM management  

#### Cons:
❌ More expensive for 24/7 workload  
❌ No built-in load balancing  
❌ Persistence requires Azure Files  

---

### Option 3: Azure Container Apps

**Description:** Serverless microservices platform built on Kubernetes.

#### Components:
- **Container App Environment**
- **Container App** (0.5 vCPU, 1 GB RAM)
- **Consumption-based pricing**

#### Pricing (West Europe region):
| Component | Calculation | Monthly Cost |
|-----------|-------------|-------------|
| vCPU (0.5) | $0.000024/vCPU-second × 1,314,000s | $31.54/month |
| Memory (1 GB) | $0.0000025/GB-second × 2,628,000s | $6.57/month |
| Requests (100K) | Included | $0.00/month |
| **TOTAL** | | **~$38/month** |

#### Pros:
✅ Kubernetes-based (scalable)  
✅ Built-in HTTPS  
✅ Integrated Dapr/KEDA  

#### Cons:
❌ Overkill for simple app  
❌ More complex than App Service  
❌ Higher cost for always-on  

---

### Option 4: Azure Virtual Machine

**Description:** Traditional IaaS VM with full control.

#### Components:
- **VM (B1s)** (1 vCPU, 1 GB RAM)
- **Managed Disk** (Standard SSD, 32 GB)
- **Public IP** (basic)

#### Pricing (West Europe region):
| Component | Calculation | Monthly Cost |
|-----------|-------------|-------------|
| B1s VM | $0.0146/hour × 730h | $10.66/month |
| Managed Disk (32 GB) | $0.05/GB | $1.60/month |
| Public IP | $0.005/hour × 730h | $3.65/month |
| **TOTAL** | | **~$16/month** |

#### Pros:
✅ Full control  
✅ Affordable  
✅ Can install anything  

#### Cons:
❌ Manual management  
❌ Security patches required  
❌ No managed services  

---

## 📊 Cost Comparison Summary

### Monthly Cost Rankings (Low to High)

| Rank | Option | Cloud | Monthly Cost | Best For |
|------|--------|-------|--------------|----------|
| 🥇 1 | **Lightsail** | AWS | **$5.50** | Budget-conscious, simple setup |
| 🥈 2 | EC2 t3.micro | AWS | **$10** | Full control, downgraded current |
| 🥉 3 | **App Service B1** | Azure | **$13** | Managed PaaS, easy deployment |
| 4 | VM B1s | Azure | **$16** | Azure VM with control |
| 5 | EC2 t3.medium (current) | AWS | **$35** | Already running (over-provisioned) |
| 6 | Fargate (no ALB) | AWS | **$38** | Serverless containers |
| 7 | Container Apps | Azure | **$38** | Modern microservices |
| 8 | ACI | Azure | **$44** | Simple serverless containers |
| 9 | Fargate (with ALB) | AWS | **$56** | Production serverless with LB |
| 10 | App Runner | AWS | **$69** | Zero-ops managed service |

---

## 🏆 Recommendations

### 🥇 Best Overall: **AWS Lightsail** ($5.50/month)

**Why:**
- Cheapest option by far
- Includes 1 TB data transfer
- Simple management
- Perfect for personal projects
- Can run Docker natively
- Frankfurt region available

**Migration Steps:**
1. Create Lightsail instance (Frankfurt)
2. Copy Docker image
3. Mount persistent volume for SQLite
4. Configure static IP
5. Point domain (optional)

---

### 🥈 Best Managed Platform: **Azure App Service B1** ($13/month)

**Why:**
- Zero server management
- Built-in Docker support
- Free SSL + custom domains
- Application Insights included
- Easy CI/CD integration
- West Europe region

**Migration Steps:**
1. Create App Service (Linux, B1)
2. Configure Docker Hub deployment
3. Add environment variables
4. Enable continuous deployment
5. Configure custom domain

---

### 🥉 Best for Current Setup: **AWS EC2 t3.micro** ($10/month)

**Why:**
- Already on AWS
- Familiar environment
- Simple downgrade from t3.medium
- Saves $25/month immediately
- No migration needed

**Optimization Steps:**
1. Take snapshot of current instance
2. Create t3.micro instance
3. Restore data and Docker containers
4. Test thoroughly
5. Terminate old instance

---

## 💡 Cost Optimization Tips

### For AWS Lightsail:
- Use instance snapshots for backups ($0.05/GB/month)
- Enable automatic snapshots (7-day retention)
- Monitor data transfer (stays within 1 TB)

### For Azure App Service:
- Use deployment slots for testing (free on Basic+)
- Enable application insights sampling (stay in free tier)
- Schedule auto-scale (scale down at night if needed)

### For Current AWS EC2:
- Downgrade to t3.micro (1 vCPU, 1 GB RAM) - sufficient
- Use gp3 instead of gp2 EBS (20% cheaper)
- Set up CloudWatch alarms for cost monitoring
- Consider Reserved Instances for 1-year commitment (save 30%)

---

## 🔍 Feature Comparison Matrix

| Feature | Lightsail | App Service | EC2 t3.micro | Fargate |
|---------|-----------|-------------|--------------|---------|
| **Cost/month** | $5.50 | $13 | $10 | $38 |
| **Ease of Setup** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ |
| **Managed OS** | ❌ | ✅ | ❌ | ✅ |
| **Auto Scaling** | ❌ | ✅ | ❌ | ✅ |
| **Docker Support** | ✅ | ✅ | ✅ | ✅ |
| **Persistent Storage** | ✅ | ✅ | ✅ | ⚠️ (EFS) |
| **Monitoring** | Basic | Advanced | CloudWatch | CloudWatch |
| **SSL/HTTPS** | Manual | Included | Manual | ALB required |
| **CI/CD** | Manual | Built-in | Manual | ECR + Pipeline |
| **Backup** | Snapshots | Built-in | Snapshots | External |

---

## 🎯 Final Recommendation

### For Your Use Case: **AWS Lightsail** 🏆

**Total Monthly Cost: $5.50**

**Reasoning:**
1. **Budget-friendly:** Saves $29.50/month vs current EC2 setup
2. **Sufficient resources:** 1 GB RAM, 1 vCPU perfect for this app
3. **Simple management:** No complex DevOps required
4. **Docker-ready:** Can deploy your existing container directly
5. **European region:** Frankfurt available
6. **Predictable costs:** No surprise bills

**Annual Savings:** $354/year compared to current setup

**ROI:** Allows you to run this project for nearly 6 years for the cost of 1 year on current EC2!

---

## 📋 Migration Plan (Lightsail)

### Phase 1: Preparation (30 minutes)
1. ✅ Create Lightsail account/instance (Frankfurt)
2. ✅ Set up static IP
3. ✅ Configure firewall (port 8080/443)

### Phase 2: Deployment (1 hour)
1. ✅ Install Docker on Lightsail instance
2. ✅ Copy flight-tracker container
3. ✅ Configure persistent volume for SQLite
4. ✅ Set up Sentry environment variables

### Phase 3: Testing (30 minutes)
1. ✅ Verify app functionality
2. ✅ Test background jobs
3. ✅ Check Sentry integration
4. ✅ Monitor resource usage

### Phase 4: Cutover (15 minutes)
1. ✅ Update DNS (if using custom domain)
2. ✅ Terminate old EC2 instance
3. ✅ Celebrate $30/month savings! 🎉

---

## 🛡️ Security Considerations

All options include:
- ✅ HTTPS (via Let's Encrypt or cloud provider)
- ✅ Firewall rules (restrict to necessary ports)
- ✅ Automatic security patches (managed options)
- ✅ Sentry error tracking (already configured)
- ✅ SQLite in-container (no exposed DB ports)

---

## 📞 Support

For questions about:
- **AWS:** AWS Support (Basic tier free)
- **Azure:** Azure Support (Basic tier free)
- **Lightsail:** Community forums + documentation

---

**Generated by Pepe 🐸 for Felipe**  
**Date:** 2026-02-10  
**Next Review:** When traffic/requirements change
