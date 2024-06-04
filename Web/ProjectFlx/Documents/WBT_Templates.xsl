<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:xs="http://www.w3.org/2001/XMLSchema"
    exclude-result-prefixes="xs"
    version="1.0">
    
<!--
        <jsTemplates>
          <jsTemplate>
              <jsName></jsName>
              <jsContainer></jsContainer>
              <jsRepeater></jsRepeater>
          </jsTemplate>
        </jsTemplates>
    -->
    
    <xsl:template match="jsTemplates" mode="identity-translate">
        <div class="js-templates">
            <xsl:apply-templates select="jsTemplate" mode="identity-translate.js"/>
        </div>
    </xsl:template>
    
    <xsl:template match="jsTemplate" mode="identity-translate.js">
        <div data-template-name="{jsName}" class="js-template-item">
            <xsl:apply-templates select="jsContainer | jsRepeater"  mode="identity-translate.js"/>
        </div>
    </xsl:template>
    
    <xsl:template match="jsGroups" mode="identity-translate"/>
    <xsl:template match="jsGroups" mode="identity-translate.js"/>
    <xsl:template match="jsName" mode="identity-translate.js"/>
    <xsl:template match="jsContainer" mode="identity-translate.js">
        <div class="js-template-container">
            <xsl:if test="jsGroups">
                <xsl:attribute name="data-js-groups">
                    <xsl:value-of select="jsGroups"/>
                </xsl:attribute>
            </xsl:if>
            <xsl:apply-templates mode="identity-translate"/>
        </div>
    </xsl:template>
    
    <xsl:template match="jsRepeater" mode="identity-translate"/>
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

    <xsl:template match="jsInsert[parent::Body | parent::body]" mode="identity-translate">
        <xsl:variable name="class" select="parent::*/@class"/>
            <xsl:attribute name="class">
                <xsl:if test="string($class)">
                    <xsl:value-of select="$class"/>
                    <xsl:text> </xsl:text>
                </xsl:if>        
                <xsl:text>js-template-insert</xsl:text>
            </xsl:attribute>
    </xsl:template>

    <xsl:template match="jsInsert" mode="identity-translate">
        <div class="js-template-insert"/>    
    </xsl:template>
    
</xsl:stylesheet>