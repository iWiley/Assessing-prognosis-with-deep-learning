warning("Please manually run the code line by line and save the image from the preview box.")
source("00.Functions.R")
# We need to get the cutoff value
source("01.CalculateCutoff.R")

load("Data/TCGA.LIHC.Counts.RData")
OrginalData.TCGA <- read_excel("Data/OrginalData.TCGA.xlsx")
names = gsub("-", ".", OrginalData.TCGA$Name)
data.counts = TCGA.LIHC.Counts[,substr(colnames(TCGA.LIHC.Counts),14,14)  == "0"]
data.counts = data.counts[,substr(colnames(data.counts),1,12) %in% names]
data.counts = IDTran(data.counts)
data.counts = data.frame(t(data.counts))
data.counts$Name = substr(rownames(data.counts), 1, 12)
  
data.Group = data.frame(
  Name = OrginalData.TCGA$Name, 
  TIL = OrginalData.TCGA$prec.TIL
)
data.Group$TIL = ifelse(data.Group$TIL < res.cut.TCGA$prec.TIL$estimate, "Low TIL", "High TIL")
data.Group$Name = gsub("-",".",data.Group$Name)

data.counts = merge(data.counts, data.Group, by = "Name")
data.counts$Group = data.counts$TIL
data.counts$Name = data.counts$TIL = NULL

result.deg = edgeR(data.counts)
result.deg = subset(result.deg, result.deg$sig != "none")

GO(rownames(result.deg), type = "BP")
GO(rownames(result.deg), type = "CC")
GO(rownames(result.deg), type = "MF")
GO(rownames(result.deg), type = "DO")
Enrichment(result.deg)