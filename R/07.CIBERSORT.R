warning("Please manually run the code line by line and save the image from the preview box.")
source("00.Functions.R")
# We need to get the cutoff value
source("01.CalculateCutoff.R")

load("Data/TCGA.LIHC.Counts.RData")
OrginalData.TCGA <- read_excel("Data/OrginalData.TCGA.xlsx")
names = gsub("-", ".", OrginalData.TCGA$Name)
data.counts = TCGA.LIHC.Counts[,substr(colnames(TCGA.LIHC.Counts),14,14)  == "0"]
data.counts = data.counts[,substr(colnames(data.counts),1,12) %in% names]
data.counts = log2(data.counts + 1)
data.counts = IDTran(data.counts)

result.cibersoft = CIBERSORT(data.counts)
result.cibersoft = data.frame(result.cibersoft)
result.cibersoft$Name = substr(rownames(result.cibersoft), 1, 12)

cibersoft.Group = data.frame(
  Name = OrginalData.TCGA$Name, 
  TIL = OrginalData.TCGA$prec.TIL
)
cibersoft.Group$TIL = ifelse(cibersoft.Group$TIL < res.cut.TCGA$prec.TIL$estimate, "Low TIL", "High TIL")
cibersoft.Group$Name = gsub("-",".",cibersoft.Group$Name)

result.cibersoft = merge(result.cibersoft, cibersoft.Group, by = "Name")
result.cibersoft$Name = NULL
result.cibersoft$Group = result.cibersoft$TIL
result.cibersoft$TIL = NULL
CIBERSORT_VIOLIN(result.cibersoft)