# This file contains the common functions
CheckPackage = function(packageName){
  if (!require("BiocManager")) {
    install.packages("BiocManager")
  }
  for (name in packageName) {
    if (!require(name, character.only = TRUE)) {
      BiocManager::install(name)
      library(name, character.only = TRUE)
    }
  }
}
Plot.KM = function(data, time = NA, title = ""){
  CheckPackage(c("dplyr","survival","survminer"))
  data <- data.frame(data)
  if (max(data$Time) > 1000) {
    data$Time = data$Time / 365
  }
  if (!is.na(time)) {
    data$Status = ifelse(data$Time > time, ifelse(data$Status == 1, 0 ,data$Status), data$Status)
    data$Time = ifelse(data$Time > time, time , data$Time)
  }
  fitt <- surv_fit(Surv(data$Time, data$Status) ~ Group, data = data )
  ptpv <- as.character(survminer::surv_pvalue(fitt)$pval)
  lab = names(table(data$Group))
  if (is.na(time)) {
    pt <- ggsurvplot(fitt,
                     data = data,
                     conf.int = TRUE,
                     pval = TRUE,
                     pval.method = TRUE,
                     legend.title = "Group",
                     legend.labs = c(lab[1], lab[2]),
                     xlab = "Time (year)",
                     surv.median.line = "hv",
                     risk.table = TRUE,
                     title = title
    ) 
  } else {
    pt <- ggsurvplot(fitt,
                     data = data,
                     conf.int = TRUE,
                     pval = TRUE,
                     pval.method = TRUE,
                     legend.title = "Group",
                     legend.labs = c(lab[1], lab[2]),
                     xlab = "Time (year)",
                     surv.median.line = "hv",
                     risk.table = TRUE,
                     xlim = c(0, time),
                     title = title
    ) 
  }
  cat(paste("p-value:",ptpv,"\n"))
  pt
}
Cox = function(data, type, plot = F) {
  CheckPackage(c("survival","survminer","forestplot","grid"))
  formatNum <- function(x, showmin = T) {
    return(ifelse(
      showmin,
      ifelse(x < .001, "< 0.001", sprintf("%0.3f", x)),
      ifelse(x < .001, "0.001", sprintf("%0.3f", x))
    ))
  }
  UnivariateCox <- function(data, isMix) {
    options(scipen = 200, digits = 3)
    data = data.frame(data)
    time <- as.numeric(data$Time)
    status <- as.numeric(data$Status)
    data$Time = data$Status = NULL
    data = lapply(data, function(x) {
      if (class(x) == "character") {
        x = as.factor(x)
      }
      return(x)
    })
    data = data.frame(data)
    univ_formulas <-
      sapply(colnames(data), function(x)
        as.formula(paste("Surv(time, status) ~ data$", x)))
    univ_models <-
      lapply(univ_formulas, function(x) {
        coxph(x, data = data)
      })
    univ_results <- lapply(univ_models, function(x) {
      levels = x$xlevels[[1]]
      name = x$terms[[3]][[3]]
      x <- summary(x)
      p.total <- formatNum(x$wald["pvalue"])
      if (is.null(levels)) {
        HR <- formatNum(x$coef[2], F)
        HR.confint.lower <- formatNum(x$conf.int[, "lower .95"], F)
        HR.confint.upper <- formatNum(x$conf.int[, "upper .95"], F)
        res <-
          c(as.character(name),
            "",
            p.total,
            HR,
            HR.confint.lower,
            HR.confint.upper)
        names(res) <-
          c("name", "faturename", "p.value", "HR", "lHR", "uHR")
        return(list(res))
      }
      HR.CI95 = x[["conf.int"]]
      P = x[["coefficients"]]
      res <- c(as.character(name), "", p.total, "", "", "")
      names(res) <-
        c("name", "faturename", "p.value", "HR", "lHR", "uHR")
      sublevels = list()
      sublevels = append(sublevels, list(res))
      i = 1
      for (variable in levels) {
        if (i == 1) {
          res <- c("", variable, "", "", "", "")
          names(res) <-
            c("name", "faturename", "p.value", "HR", "lHR", "uHR")
          sublevels = append(sublevels, list(res))
        } else {
          res <- c(
            "",
            variable,
            formatNum(P[, "Pr(>|z|)"][i - 1]),
            formatNum(HR.CI95[, "exp(coef)"][i - 1], F),
            formatNum(HR.CI95[, "lower .95"][i - 1], F),
            formatNum(HR.CI95[, "upper .95"][i - 1], F)
          )
          names(res) <-
            c("name", "faturename", "p.value", "HR", "lHR", "uHR")
          sublevels = append(sublevels, list(res))
        }
        i = i + 1
      }
      return(sublevels)
    })
    res = list()
    needmuti = c()
    for (item in univ_results) {
      res = append(res, item)
      if (item[[1]][["p.value"]] != "") {
        if (item[[1]][["p.value"]] == "< 0.001") {
          needmuti = append(needmuti, item[[1]][["name"]])
        } else if (as.numeric(item[[1]][["p.value"]]) < .05) {
          needmuti = append(needmuti, item[[1]][["name"]])
        }
      }
    }
    res = data.frame(t(data.frame(res)))
    rownames(res) = c(1:length(rownames(res)))
    hCenter = as.numeric(res$HR)
    hLow = as.numeric(res$lHR)
    hHigh = as.numeric(res$uHR)
    CI95 = c()
    for (i in 1:length(rownames(res))) {
      r = res[i,]
      if (is.na(r["p.value"])) {
        CI95 = append(CI95, "")
      } else if (r["p.value"] == "") {
        CI95 = append(CI95, "Reference")
      } else if (r["faturename"] == "" & r["HR"] == "") {
        CI95 = append(CI95, "")
      } else{
        CI95 = append(CI95, paste(r["lHR"], "-", r["uHR"]))
      }
    }
    tabletext <- cbind(
      c("Item", res$name),
      c("", res$faturename),
      c("HR (95% CI for HR)", CI95),
      c("P Value", res$p.value)
    )
    fp = forestplot(
      title = "Univariate analysis",
      xticks = seq(from = 0, to = 7, by = 1),
      clip = c(0 , 8),
      hrzl_lines = gpar(col = "#444444"),
      is.summary = c(TRUE, rep(FALSE, length(CI95))),
      mean = c(NA, hCenter),
      lower = c(NA, hLow),
      upper = c(NA, hHigh),
      zero = 1,
      labeltext = tabletext,
      graph.pos = 3,
      graphwidth = unit(80, "mm"),
      vertices = TRUE,
      grid = TRUE,
      boxsize = 0.2,
      new_page = !isMix
    )
    return(list(
      result = res,
      plot = fp,
      sig = needmuti,
      tabletext = tabletext
    ))
  }
  MultivariateCox <- function(data, covariates, isMix) {
    options(scipen = 200, digits = 3)
    res.u = UnivariateCox(data, F)
    tabletext <- res.u$tabletext
    data = data.frame(data)
    time <- as.numeric(data$Time)
    status <- as.numeric(data$Status)
    data$Time = data$Status = NULL
    data = lapply(data, function(x) {
      if (class(x) == "character") {
        x = as.factor(x)
      }
      return(x)
    })
    data = data.frame(data)
    if (is.null(covariates)) {
      covariates = colnames(data)
    }
    exp = parse(text = paste(
      "coxph(Surv(time, status) ~ ",
      paste(covariates, collapse = "+"),
      ", data = data)"
    ))
    result = eval(exp)
    factors = result[["xlevels"]]
    result = summary(result)
    P = result[["coefficients"]]
    x = lapply(rownames(P), function(x) {
      for (variable in names(factors)) {
        if (startsWith(x, variable)) {
          for (vlevel in factors[[variable]]) {
            if (x == paste0(variable, vlevel)) {
              return(variable)
            }
          }
        }
      }
    })
    rownames(P) = as.character(x)
    HR = result[["conf.int"]]
    rownames(HR) = as.character(x)
    sublevels = list()
    i = 1
    for (f in covariates) {
      if (f %in% covariates) {
        levels = factors[[f]]
        p.total = formatNum(P[f,]["Pr(>|z|)"], T)
        if (is.null(levels)) {
          HRv <- formatNum(HR[f,]["exp(coef)"], F)
          HR.confint.lower <- formatNum(HR[f,]["lower .95"], F)
          HR.confint.upper <- formatNum(HR[f,]["upper .95"], F)
          res <-
            c(f,
              "",
              p.total,
              HRv,
              HR.confint.lower,
              HR.confint.upper)
          names(res) <-
            c("name", "faturename", "p.value", "HR", "lHR", "uHR")
          sublevels = append(sublevels, list(res))
        } else {
          res <- c(f, "", "", "", "", "")
          names(res) <-
            c("name", "faturename", "p.value", "HR", "lHR", "uHR")
          sublevels = append(sublevels, list(res))
          res <- c(f, levels[1], "", "", "", "")
          names(res) <-
            c("name", "faturename", "p.value", "HR", "lHR", "uHR")
          sublevels = append(sublevels, list(res))
          HRv <- formatNum(HR[f,]["exp(coef)"], F)
          HR.confint.lower <- formatNum(HR[f,]["lower .95"], F)
          HR.confint.upper <- formatNum(HR[f,]["upper .95"], F)
          res <-
            c(f,
              levels[2],
              p.total,
              HRv,
              HR.confint.lower,
              HR.confint.upper)
          names(res) <-
            c("name", "faturename", "p.value", "HR", "lHR", "uHR")
          sublevels = append(sublevels, list(res))
        }
      }
    }
    res = data.frame(t(data.frame(sublevels)))
    rownames(res) = c(1:length(rownames(res)))
    re.res = res
    hCenter = c(rep(NA, length(tabletext[, 1])))
    hLow = c(rep(NA, length(tabletext[, 1])))
    hHigh = c(rep(NA, length(tabletext[, 1])))
    index = c(rep(NA, length(tabletext[, 1])))
    CI95 = c(rep(NA, length(tabletext[, 1])))
    p = c(rep(NA, length(tabletext[, 1])))
    i = 0
    for (item in tabletext[, 1]) {
      i = i + 1
      if (item != "") {
        if (item %in% res[["name"]]) {
          index[i] = item
          subD = res[res["name"] == item,]
          n = 0
          rn = length(subD[, 1]) > 1
          while (length(subD[, 1]) > 0) {
            if (subD[1,][3] == "") {
              p [i + n] = "-"
            } else {
              p [i + n] = subD[1,][3]
            }
            if (subD[1,][3] == "") {
              if (n != 0) {
                CI95[i + n] = "Reference"
              }
            } else {
              CI95[i + n] = paste(subD[1,][5], "-", subD[1,][6])
            }
            if (is.null(subD[1,][4])) {
              hCenter[i + n] = NA
            } else {
              hCenter[i + n] = subD[1,][4]
            }
            if (is.null(subD[1,][5])) {
              hLow[i + n] = NA
            } else {
              hLow[i + n] = subD[1,][5]
            }
            if (is.null(subD[1,][6])) {
              hHigh[i + n] = NA
            } else {
              hHigh[i + n] = subD[1,][6]
            }
            subD = subD[-1,]
            n = n + 1
          }
          for (x in 1:n) {
            res = res[-1,]
          }
        }
      }
    }
    app = function(x) {
      if (is.null(x)) {
        return(NA)
      }
      return(x)
    }
    hCenter = lapply(hCenter, app)
    hCenter = as.numeric(hCenter)
    hLow = lapply(hLow, app)
    hLow = as.numeric(hLow)
    hHigh = lapply(hHigh, app)
    hHigh = as.numeric(hHigh)
    hCenter = hCenter[2:length(hCenter)]
    hLow = hLow[2:length(hLow)]
    hHigh = hHigh[2:length(hHigh)]
    p = lapply(p, app)
    CI95 = lapply(CI95, app)
    p = unlist(p)
    CI95 = unlist(CI95)
    CI95[1] = "HR (95% CI for HR)"
    p[1] = "P Value"
    index[1] = "Item"
    tabletext2 <-
      cbind(c("Item", res.u$result$name),
            c("", res.u$result$faturename),
            c(CI95),
            c(p))
    if (isMix) {
      tabletext2 <- cbind(c(CI95), c(p))
    }
    picm <- forestplot(
      title = "Multivariate analysis",
      xticks = seq(from = 0, to = 7, by = 1),
      clip = c(0 , 8),
      hrzl_lines = gpar(col = "#444444"),
      is.summary = c(TRUE, rep(FALSE, length(CI95))),
      mean = c(NA, hCenter),
      lower = c(NA, hLow),
      upper = c(NA, hHigh),
      zero = 1,
      labeltext = tabletext2,
      graph.pos = ifelse(isMix, 1, 3),
      graphwidth = unit(80, "mm"),
      vertices = TRUE,
      grid = TRUE,
      boxsize = 0.2,
      new_page = !isMix
    )
    return(list(result = re.res, plot = picm))
  }
  if (type %in% c("u", "U", "univariate", "Univariate")) {
    r = UnivariateCox(data, F)
    if (plot) {
      plot(r$plot)
    }
    return(r$result)
  }
  if (type %in% c("m", "M", "multivariate", "Multivariate")) {
    r = MultivariateCox(data, NULL, F)
    if (plot) {
      plot(r$plot)
    }
    return(r$result)
  }
  if (type %in% c("mix", "Mix")) {
    u = UnivariateCox(data, T)
    m = MultivariateCox(data, u$sig, T)
    if (!plot) {
      return(list(
        UnivariateCox = u$result,
        MultivariateCox = m$result
      ))
    }
    grid.newpage()
    borderWidth <- unit(4, "pt")
    width <-
      unit(convertX(
        unit(1, "npc") - borderWidth,
        unitTo = "npc",
        valueOnly = TRUE
      ) / 2,
      "npc")
    pushViewport(viewport(layout = grid.layout(nrow = 1, ncol = 2)))
    pushViewport(viewport(layout.pos.row = 1, layout.pos.col = 1))
    plot(u$plot)
    upViewport()
    pushViewport(viewport(layout.pos.row = 1, layout.pos.col = 2))
    plot(m$plot)
    upViewport()
    return(list(
      UnivariateCox = u$result,
      MultivariateCox = m$result
    ))
  }
  stop("type error")
}
TimeROC = function(data, title = "ROC Curve", method = "KM", time = c(1,2,5), showCutOff = T){
  CheckPackage(c("ggnewscale","survivalROC","ggplot2"))
  data = data.frame(data)
  if (max(data$Time) > 100) {
    data$Time = data$Time / 365
  }
  data.roc = data.frame()
  for (t in time) {
    roc <- survivalROC(
      Stime = data$Time,
      status = data$Status,
      marker = data$Factor,
      predict.time = t,
      span = 0.01*NROW(data)^(-0.20),
      method = method)
    cutoff <- roc$cut.values[which.max(roc$TP-roc$FP)]
    cutoff_y <- roc$TP[roc$cut.values==cutoff]
    cutoff_x <- roc$FP[roc$cut.values==cutoff]
    auc = as.character(round(roc$AUC,3))
    cat(paste("AUC of",t ,"year(s) =",auc , ", Cutoff =",cutoff ,"\n"))
    roc = data.frame(
      FP = roc$FP,
      TP = roc$TP,
      AUC = auc,
      cutoff_y = cutoff_y,
      cutoff_x = cutoff_x,
      Time = paste0(ifelse(t == 1, "1 year", paste(as.character(t), "years")), " = ", auc),
      Time_Cut = paste0(ifelse(t == 1, "1 year", paste(as.character(t), "years")), " = ", round(cutoff,3))
    )
    data.roc = rbind(data.roc, roc)
  }
  if (showCutOff) {
    ggplot(data.roc, aes(x = FP, y = TP, color = Time)) + 
      scale_colour_discrete(name = "Cutoff", labels = unique(data.roc$Time_Cut)) +
      geom_abline(intercept = 0, slope = 1, color = "gray", linetype = "dashed") +
      geom_point(aes(x = cutoff_x, y = cutoff_y, group = Time)) +
      new_scale_colour() +
      geom_line(key_glyph = "timeseries", aes(color = Time)) +
      scale_colour_discrete(name = "AUC") +
      xlab("False positive rate") +
      ylab("True positive rate") +
      ggtitle(title) 
  } else {
    ggplot(data.roc, aes(x = FP, y = TP, color = Time)) + 
      geom_abline(intercept = 0, slope = 1, color = "gray", linetype = "dashed") +
      geom_line(key_glyph = "timeseries", aes(color = Time)) +
      scale_colour_discrete(name = "AUC") +
      xlab("False positive rate") +
      ylab("True positive rate") +
      ggtitle(title) 
  }
}
CIBERSORT = function(mixture_file, perm=0, QN=F){
  CoreAlg <- function(X, y){
    
    #try different values of nu
    svn_itor <- 3
    
    res <- function(i){
      if(i==1){nus <- 0.25}
      if(i==2){nus <- 0.5}
      if(i==3){nus <- 0.75}
      model<-e1071::svm(X,y,type="nu-regression",kernel="linear",nu=nus,scale=F)
      model
    }
    
    if(Sys.info()['sysname'] == 'Windows') out <- parallel::mclapply(1:svn_itor, res, mc.cores=1) else
      out <- parallel::mclapply(1:svn_itor, res, mc.cores=svn_itor)
    
    nusvm <- rep(0,svn_itor)
    corrv <- rep(0,svn_itor)
    
    #do cibersort
    t <- 1
    while(t <= svn_itor) {
      weights = t(out[[t]]$coefs) %*% out[[t]]$SV
      weights[which(weights<0)]<-0
      w<-weights/sum(weights)
      u <- sweep(X,MARGIN=2,w,'*')
      k <- apply(u, 1, sum)
      nusvm[t] <- sqrt((mean((k - y)^2)))
      corrv[t] <- cor(k, y)
      t <- t + 1
    }
    
    #pick best model
    rmses <- nusvm
    mn <- which.min(rmses)
    model <- out[[mn]]
    
    #get and normalize coefficients
    q <- t(model$coefs) %*% model$SV
    q[which(q<0)]<-0
    w <- (q/sum(q))
    
    mix_rmse <- rmses[mn]
    mix_r <- corrv[mn]
    
    newList <- list("w" = w, "mix_rmse" = mix_rmse, "mix_r" = mix_r)
    
  }
  doPerm <- function(perm, X, Y){
    itor <- 1
    Ylist <- as.list(data.matrix(Y))
    dist <- matrix()
    
    while(itor <= perm){
      #print(itor)
      
      #random mixture
      yr <- as.numeric(Ylist[sample(length(Ylist),dim(X)[1])])
      
      #standardize mixture
      yr <- (yr - mean(yr)) / sd(yr)
      
      #run CIBERSORT core algorithm
      result <- CoreAlg(X, yr)
      
      mix_r <- result$mix_r
      
      #store correlation
      if(itor == 1) {dist <- mix_r}
      else {dist <- rbind(dist, mix_r)}
      
      itor <- itor + 1
    }
    newList <- list("dist" = dist)
  }
  
  #read in data
  X <- read.table("https://public.wistu.cn/rs/LM22.txt", header=T, sep="\t",row.names=1,check.names=F)
  # Y <- read.table(mixture_file, header=T, sep="\t", row.names=1,check.names=F)
  
  X <- data.matrix(X)
  Y <- data.matrix(mixture_file)
  
  #order
  X <- X[order(rownames(X)),]
  Y <- Y[order(rownames(Y)),]
  
  P <- perm #number of permutations
  
  #anti-log if max < 50 in mixture file
  if(max(Y) < 50) {Y <- 2^Y}
  
  #quantile normalization of mixture file
  if(QN == TRUE){
    tmpc <- colnames(Y)
    tmpr <- rownames(Y)
    Y <- preprocessCore::normalize.quantiles(Y)
    colnames(Y) <- tmpc
    rownames(Y) <- tmpr
  }
  
  #intersect genes
  Xgns <- row.names(X)
  Ygns <- row.names(Y)
  YintX <- Ygns %in% Xgns
  Y <- Y[YintX,]
  XintY <- Xgns %in% row.names(Y)
  X <- X[XintY,]
  
  #standardize sig matrix
  X <- (X - mean(X)) / sd(as.vector(X))
  
  #empirical null distribution of correlation coefficients
  if(P > 0) {nulldist <- sort(doPerm(P, X, Y)$dist)}
  
  #print(nulldist)
  
  header <- c('Mixture',colnames(X),"P-value","Correlation","RMSE")
  #print(header)
  
  output <- matrix()
  itor <- 1
  mixtures <- dim(Y)[2]
  pval <- 9999
  
  #iterate through mixtures
  while(itor <= mixtures){
    
    y <- Y[,itor]
    
    #standardize mixture
    y <- (y - mean(y)) / sd(y)
    
    #run SVR core algorithm
    result <- CoreAlg(X, y)
    
    #get results
    w <- result$w
    mix_r <- result$mix_r
    mix_rmse <- result$mix_rmse
    
    #calculate p-value
    if(P > 0) {pval <- 1 - (which.min(abs(nulldist - mix_r)) / length(nulldist))}
    
    #print output
    out <- c(colnames(Y)[itor],w,pval,mix_r,mix_rmse)
    if(itor == 1) {output <- out}
    else {output <- rbind(output, out)}
    
    itor <- itor + 1
    
  }
  
  #save results
  # write.table(rbind(header,output), file="CIBERSORT-Results.txt", sep="\t", row.names=F, col.names=F, quote=F)
  
  #return matrix object containing all results
  obj <- rbind(header,output)
  obj <- obj[,-1]
  obj <- obj[-1,]
  obj <- matrix(as.numeric(unlist(obj)),nrow=nrow(obj))
  rownames(obj) <- colnames(Y)
  colnames(obj) <- c(colnames(X),"P-value","Correlation","RMSE")
  obj
}
CIBERSORT_VIOLIN = function(data, title = "CIBERSORT"){
  CheckPackage(c("ggplot2","tidyr","ggpubr"))
  g = data$Group
  data$Group = NULL
  data$Group = g
  dt1 <- data %>% 
    as.data.frame() %>% 
    tibble::rownames_to_column("sample") %>% 
    pivot_longer(cols=2:23,
                 names_to= "celltype",
                 values_to = "Proportion")
  ggplot(
    data = dt1,
    aes(x = celltype,y = Proportion, fill=Group),
    xlab = "",
    ylab = "Cell composition",
    main = title
  ) +   
    geom_violin(trim = T, scale = "width") +
    geom_boxplot(position = position_dodge(0.9), width = 0.2) +
    stat_compare_means(
      aes(group = Group),
      label = "p.signif",
      method = "wilcox.test",
      hide.ns = T,
      size = 4.5 
    ) + 
    theme(axis.text.x = element_text(
      angle = 45,
      hjust = 1,
      vjust = 1
    ))
}
edgeR = function(countExpData, logFC = 1, pValue = .05){
  CheckPackage(c("edgeR","limma","statmod","EnhancedVolcano"))
  if (length(table(countExpData$Group)) != 2) {
    stop("")
  }
  group = as.factor(countExpData$Group)
  design <- model.matrix(~group)
  rownames(design) = rownames(countExpData)
  countExpData$Group = NULL
  countExpData = data.frame(t(countExpData))
  countExpData = DGEList(countExpData, group = group)
  keep = edgeR::filterByExpr(countExpData, design = design)
  dgelist = countExpData[keep,,keep.lib.sizes = FALSE]
  dgelist_norm <- edgeR::calcNormFactors(dgelist)
  dgelist_norm = estimateDisp(dgelist_norm, design, robust = TRUE)
  et<- exactTest(dgelist_norm)
  dge_de <- decideTestsDGE(et, adjust.method = 'fdr', p.value = pValue)   #查看默认方法获得的差异基因
  print(summary(dge_de))
  result = topTags(et, n = nrow(et$table))$table
  gene_diff = data.frame(result)
  gene_diff[which(gene_diff$logFC >= logFC & gene_diff$FDR < pValue), "sig"] <- "up"
  gene_diff[which(gene_diff$logFC <= -logFC & gene_diff$FDR < pValue), "sig"] <- "down"
  gene_diff[which(abs(gene_diff$logFC) < logFC | gene_diff$FDR >= pValue), "sig"] <- "none"
  table(gene_diff$sig)
  p = EnhancedVolcano(
    result,
    lab = rownames(result),
    x = "logFC",
    y = "PValue",
    title = "Differential expression analysis",
    subtitle = "",
    pCutoff = pValue,
    FCcutoff = logFC,
    col = c("black", "blue", "green", "red"),
    colAlpha = 1,
    legendPosition = "right",
    legendLabSize = 14,
    legendIconSize = 5.0
  )
  pic.volcano <<- p
  return(gene_diff)
}
GO = function(genes, type = "BP", geneType = "auto", value = "p.adjust") {
  CheckPackage(c("clusterProfiler","org.Hs.eg.db","enrichplot"))
  if (geneType == "auto") {
    if (substr(genes[[1]], 1, 4) == "ENSG") {
      geneType = "ensg"
    } else {
      geneType = "symbol"
    }
  }
  geneType = ifelse(geneType == "ensg", "ENSEMBL", "SYMBOL")
  genes <- substr(genes, 0, 15)
  if (type == "DO") {
    if (!require(DOSE)) {
      install.packages("DOSE")
    }
    library(DOSE)
    geneid <- mapIds(
      x = org.Hs.eg.db,
      keys = genes,
      keytype = geneType,
      column = "ENTREZID"
    )
    goDO <- enrichDO(
      gene = geneid,
      ont = "DO",
      pvalueCutoff = 0.5,
      qvalueCutoff = 0.5
    )
    enrichplot::dotplot(goDO, color = value)
  } else {
    go <- enrichGO(
      gene = genes,
      OrgDb = org.Hs.eg.db,
      keyType = geneType,
      ont = type,
      pvalueCutoff = 0.5,
      qvalueCutoff = 0.5
    )
    enrichplot::dotplot(go, color = value)
  }
}
Enrichment = function(deg, genes = NULL, geneType = "auto", pCutoff = 0.05){
  CheckPackage(c("clusterProfiler","ReactomePA","pathview","enrichplot"))
  if (!is.null(genes)) {
    deg = subset(deg, rownames(deg) %in% genes)
  }
  logFC = data.frame(
    Genes = rownames(deg),
    logFC = deg$logFC
  )
  if (geneType == "auto") {
    if (substr(rownames(deg)[[1]], 1, 4) == "ENSG") {
      geneType = "ensg"
    } else {
      geneType = "symbol"
    }
  }
  geneType = ifelse(geneType == "ensg", "ENSEMBL", "SYMBOL")
  gene = bitr(logFC$Genes, fromType=geneType,toType="ENTREZID",OrgDb="org.Hs.eg.db") 
  colnames(gene)[which(colnames(gene) == geneType)] = "Genes"
  gene = merge(gene, logFC, by = "Genes")
  gene = gene[order(gene$logFC, decreasing = T),]
  logFC = gene$logFC
  names(logFC) = gene$ENTREZID
  k = gseKEGG(
    logFC,
    keyType  = 'ncbi-geneid',
    organism = 'hsa',
    pvalueCutoff = pCutoff,
    pAdjustMethod = "none"
  )
  kegg = NULL
  x = nrow(k@result)
  if (x == 0) {
    cat("The given list of genes was not enriched to any pathway in the KEGG database.\n")
  } else {
    cat(paste("The given list of genes in the KEGG database is enriched to ", x ," pathways.\n"))
    if (x > 10) {
      x = 10
    }
    kegg = gseaplot2(k, 1:x, pvalue_table = T)
  }
  kk = gsePathway(logFC, pvalueCutoff = pCutoff )
  x = nrow(kk@result)
  reactome = NULL
  if (x == 0) {
    cat("The given list of genes was not enriched to any pathway in the Reactome database.\n")
  } else {
    cat(paste("The given gene list was enriched to ", x ," pathways in the Reactome database.\n"))
    if (x > 10) {
      x = 10
    }
    reactome = gseaplot2(kk, 1:x,pvalue_table = T )
  }
  return(list(KEGG = kegg, Reactome = reactome))
}
IDTran = function(data){
  CheckPackage(c("clusterProfiler","org.Hs.eg.db"))
  fromType = "ENSEMBL"
  toType="SYMBOL"
  data$ENSEMBL = rownames(data)
  data$ENSEMBL = substr(data$ENSEMBL, 1, 15)
  IDTansfer = bitr(data$ENSEMBL, fromType=fromType,toType=toType,OrgDb="org.Hs.eg.db") 
  data = merge(data, IDTansfer, by = fromType)
  data$ENSEMBL = NULL
  data = aggregate(.~SYMBOL, data = data, mean)
  rownames(data) = data$SYMBOL
  data$SYMBOL = NULL 
  return(data)
}
Plot.FrameBox = function(data, title = "", method = "anova"){
  CheckPackage(c("ggplot2","ggpubr","ggthemes"))
  ggplot(data, aes(x = Group, y = Value, color = Group)) +
    stat_boxplot(geom = "errorbar", width = 0.15) +
    geom_boxplot() +
    geom_dotplot(binaxis = "y", stackdir = "center", dotsize = .25) +
    geom_jitter(shape = 16, position = position_jitter(0.2)) +
    labs(title = title, x = "", y = "") +
    stat_compare_means(method = method) +
    scale_color_tableau()
}