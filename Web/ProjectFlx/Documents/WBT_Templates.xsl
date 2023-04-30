<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    exclude-result-prefixes="xs"
    version="1.0">
    
    <xsl:template match="jsTemplates" mode="identity-translate">
        <div class="js-templates">
            <xsl:apply-templates select="jsTemplate" mode="identity-translate.js"/>
        </div>
    </xsl:template>
    
    <xsl:template match="jsTemplate" mode="identity-translate.js">
        <div data-template-name="{jsName}" class="js-template-item">
            <xsl:apply-templates  mode="identity-translate.js"/>
        </div>
    </xsl:template>
    
    <xsl:template match="jsName" mode="identity-translate.js"/>
    <xsl:template match="jsContainer" mode="identity-translate.js">
        <div class="js-template-container">
            <xsl:apply-templates mode="identity-translate"/>
        </div>
    </xsl:template>
    
    <xsl:template match="jsRepeater" mode="identity-translate.js">
        <xsl:choose>
            <xsl:when test="tr">
                <table class="js-template-repeater">
                    <xsl:apply-templates select="*" mode="identity-translate"/>                                
                </table>
            </xsl:when>
            <xsl:otherwise>
                <div class="js-template-repeater">
                    <xsl:apply-templates select="*" mode="identity-translate"/>            
                </div>                    
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>

    <xsl:template match="jsInsert" mode="identity-translate">
        <div class="js-template-insert"></div>    
    </xsl:template>
    
</xsl:stylesheet>