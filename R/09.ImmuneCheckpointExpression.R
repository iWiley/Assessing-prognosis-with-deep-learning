warning("Please manually run the code line by line and save the image from the preview box.")
source("00.Functions.R")
# We need to get the cutoff value
source("01.CalculateCutoff.R")

OrginalData.TCGA <- read_excel("Data/OrginalData.TCGA.xlsx")
load("Data/TCGA.LIHC.TPM.RData")
data.tpm = TCGA.LIHC.TPM[,substr(colnames(TCGA.LIHC.TPM),14,14)  == "0"]
data.tpm = log2(data.tpm + 1)
data.tpm = limma::normalizeBetweenArrays(data.tpm)
data.tpm = data.frame(data.tpm)
gene.ici = c("ENSG00000188389", "ENSG00000120217", "ENSG00000163599")
data.tpm = data.tpm[substr(rownames(data.tpm), 1, 15) %in% gene.ici,]
data.tpm = data.frame(t(data.tpm))

data.Group = data.frame(
  Name = OrginalData.TCGA$Name, 
  TIL = OrginalData.TCGA$prec.TIL
)
data.Group$TIL = ifelse(data.Group$TIL < res.cut.TCGA$prec.TIL$estimate, "Low TIL", "High TIL")
data.Group$Name = gsub("-",".",data.Group$Name)

data.tpm = subset(data.tpm, substr(rownames(data.tpm), 1, 12) %in% data.Group$Name)
data.tpm$Name = substr(rownames(data.tpm), 1, 12)
data.tpm = merge(data.tpm, data.Group, by = "Name")
data.tpm$Group = data.tpm$TIL
data.tpm$Name = data.tpm$TIL = NULL

data.tpm$Value = data.tpm$ENSG00000163599.17
Plot.FrameBox(data.tpm, title = "CTLA-4", method = "wilcox.test")
data.tpm$Value = data.tpm$ENSG00000120217.14
Plot.FrameBox(data.tpm, title = "PD-L1", method = "wilcox.test")
data.tpm$Value = data.tpm$ENSG00000188389.11
Plot.FrameBox(data.tpm, title = "PD-1", method = "wilcox.test")