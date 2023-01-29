warning("Please manually run the code line by line and save the image from the preview box.")
source("00.Functions.R")
CheckPackage(c("survival", "rms"))

# First we need to get the cutoff value
source("01.CalculateCutoff.R")
data.nomo = OrginalData.TCGA
# Columns to be retained
ret = c("Age", "Gender", "M", "T", "prec.TIL", "Time", "Status")
data.nomo = data.nomo[,colnames(data.nomo) %in% ret]
data.nomo$Age = ifelse(data.nomo$Age < 65, "<65", ">=65")
data.nomo = na.omit(data.nomo)

i = 0
for (variable in colnames(data.nomo)) {
  i = i + 1
  if (colnames(data.nomo)[i] == "prec.TIL") {
    colnames(data.nomo)[i] = "TIL(%)"
    break
  }
}

dd<-datadist(data.nomo)
options(datadist="dd")

f <- cph(Surv(Time,Status)~ Age+Gender+M+T+`TIL(%)`, data = data.nomo, x=T, y = T, surv = T)
survival <- Survival(f)
survival1 <- function(x)survival(1,x) 
survival2 <- function(x)survival(2,x) 
survival5 <- function(x)survival(5,x) 
nomo = nomogram(f, 
                fun = c(survival1, survival2, survival5), 
                fun.at = c(0.1,seq(0.1,0.7,by = 0.2),seq(0.7,0.95,by = 0.1), 0.95),
                funlabel = c("1 year survival","2 years survival","5 years survival"))
plot(nomo)